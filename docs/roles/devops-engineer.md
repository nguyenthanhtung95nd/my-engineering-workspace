# Role: DevOps — Infrastructure & Operations

> Responsible for CI/CD pipelines, infrastructure as code (CDK), deployments,
> incident response, and ensuring the system is observable and recoverable.

---

## Primary Tools

| Tool | Purpose |
|------|---------|
| `/devops` | CDK stack review, CI/CD pipeline generation, postmortem |
| `/incident` | Real-time triage when production is degraded or down |
| `/migrate` | Review schema changes for deployment safety |
| `/compliance` | Security and regulatory gap analysis before go-live |
| `/perf` | Infrastructure performance — Lambda cold start, RDS Proxy latency |

---

## Workflow

### Pre-deployment

```
1. /migrate          → Safety review of every schema change
2. /devops Task B    → Generate or review CI/CD pipeline (GitLab CI)
3. /compliance       → Security gaps before go-live
4. Deploy            → CDK deploy to staging, then production
```

### Incident response

```
1. /incident         → Triage: severity, ranked hypotheses, immediate action
2. /debug            → Root cause confirmation
3. Fix or rollback   → Restore service
4. /devops Task C    → Postmortem with action items
```

---

## Real-World Example

**Scenario:** The Product CRUD Lambda is ready to ship. Source on GitLab.
CDK stack on AWS. MySQL on RDS. Deployed to staging first, then production.

---

### Step 1 — /migrate: Schema safety before deploying

Before applying the products table migration to RDS:

```
/migrate
[paste products table SQL migration]
```

Claude confirms:

```
Migration Risk Summary
Overall Risk: 🟢 LOW

🔴 Data Loss:       None — new table only
🔴 Lock/Downtime:   None — CREATE TABLE does not lock existing tables
🟡 Breaking Changes: None — no existing code references this table yet
🟡 Performance:     INDEX idx_products_active_created — correct for list query
🟢 Rollback:        DROP TABLE products (clean, no data loss)

Deployment Instructions:
Pre-deployment:  Run migration on RDS before deploying Lambda code
Deployment order: schema first, Lambda second
Post-deployment: verify EXPLAIN on list query shows index seek

Safe Migration Checklist:
✅ No data loss in Up
✅ Rollback is clean (DROP TABLE)
✅ Index covers primary query pattern
✅ No NOT NULL columns on existing tables
```

---

### Step 2 — GitLab CI/CD pipeline

```
/devops Task B
"Generate a GitLab CI pipeline for a .NET 8 Lambda project.
 Stages: build, test, package, deploy-staging, deploy-production.
 Registry: AWS ECR.
 Deploy: CDK deploy via GitLab CI with OIDC role assumption.
 Protect production stage — manual trigger only."
```

Claude generates `.gitlab-ci.yml`:

```yaml
stages:
  - build
  - test
  - package
  - deploy-staging
  - deploy-production

variables:
  DOTNET_VERSION: "8.0"
  AWS_REGION: "ap-southeast-2"
  CDK_APP: "cdk/ProductApiStack"

build:
  stage: build
  image: mcr.microsoft.com/dotnet/sdk:8.0
  script:
    - dotnet build src/ProductFunction/ProductFunction.sln -c Release
  cache:
    key: "$CI_COMMIT_REF_SLUG"
    paths:
      - .nuget/

unit-test:
  stage: test
  image: mcr.microsoft.com/dotnet/sdk:8.0
  script:
    - dotnet test src/ProductFunction/UnitTests/ -c Release --logger "junit;LogFilePath=test-results.xml"
  artifacts:
    reports:
      junit: test-results.xml

integration-test:
  stage: test
  image: mcr.microsoft.com/dotnet/sdk:8.0
  services:
    - mysql:8.0
  variables:
    MYSQL_ROOT_PASSWORD: testpass
    MYSQL_DATABASE: testdb
    DB_CONNECTION_STRING: "Server=mysql;Database=testdb;User=root;Password=testpass;"
  script:
    - dotnet test src/ProductFunction/IntegrationTests/ -c Release

package:
  stage: package
  image: mcr.microsoft.com/dotnet/sdk:8.0
  script:
    - dotnet publish src/ProductFunction/App/ -c Release -o publish/
    - zip -j product-function.zip publish/*
  artifacts:
    paths:
      - product-function.zip

deploy-staging:
  stage: deploy-staging
  image: node:20
  id_tokens:
    GITLAB_OIDC_TOKEN:
      aud: https://gitlab.com
  script:
    - npm install -g aws-cdk
    - aws sts assume-role-with-web-identity
        --role-arn $AWS_STAGING_ROLE_ARN
        --web-identity-token $GITLAB_OIDC_TOKEN
        --role-session-name gitlab-deploy > /tmp/creds.json
    - export AWS_ACCESS_KEY_ID=$(jq -r .Credentials.AccessKeyId /tmp/creds.json)
    - export AWS_SECRET_ACCESS_KEY=$(jq -r .Credentials.SecretAccessKey /tmp/creds.json)
    - export AWS_SESSION_TOKEN=$(jq -r .Credentials.SessionToken /tmp/creds.json)
    - cdk deploy ProductApiStack-Staging --require-approval never
  environment:
    name: staging
  only:
    - main

deploy-production:
  stage: deploy-production
  image: node:20
  id_tokens:
    GITLAB_OIDC_TOKEN:
      aud: https://gitlab.com
  script:
    - npm install -g aws-cdk
    - aws sts assume-role-with-web-identity
        --role-arn $AWS_PROD_ROLE_ARN
        --web-identity-token $GITLAB_OIDC_TOKEN
        --role-session-name gitlab-deploy > /tmp/creds.json
    - export AWS_ACCESS_KEY_ID=$(jq -r .Credentials.AccessKeyId /tmp/creds.json)
    - export AWS_SECRET_ACCESS_KEY=$(jq -r .Credentials.SecretAccessKey /tmp/creds.json)
    - export AWS_SESSION_TOKEN=$(jq -r .Credentials.SessionToken /tmp/creds.json)
    - cdk deploy ProductApiStack-Production --require-approval never
  environment:
    name: production
  when: manual
  only:
    - main
```

---

### Step 3 — /incident: Production is down

Three days after launch, error rate spikes to 40% on POST /products.

```
/incident
"ERROR: Task timed out after 30.00 seconds (Lambda timeout)
 ERROR RATE: 40% on POST /products (baseline: 0%)
 STARTED: 14:23 UTC
 RECENT CHANGES: RDS Proxy connection limit reduced from 100 to 10 by infra team at 14:15 UTC"
```

Claude responds:

```
Severity: P1

Hypothesis 1 (90%): RDS Proxy connection limit too low for Lambda concurrency.
Lambda scales to N concurrent invocations, each holding a connection.
With limit = 10, the 11th concurrent request times out waiting for a connection.

Immediate action:
→ Increase RDS Proxy MaxConnectionsPercent back to previous value in CDK stack.
→ If CDK deploy takes > 5 min: manually update via AWS Console as temporary fix.

Hypothesis 2 (10%): Connection pool exhaustion in the Lambda code itself.
Less likely since the issue started exactly when the limit was changed.

Data needed to confirm Hypothesis 1:
→ CloudWatch Metrics: RDS Proxy > DatabaseConnectionsCurrentlyBorrowed
→ Expected: see connections hitting the limit at 14:23
```

DevOps increases RDS Proxy limit → error rate returns to 0%.

---

### Step 4 — /devops Task C: Postmortem

```
/devops Task C
"Timeline:
 14:15 — RDS Proxy MaxConnectionsPercent reduced from 50 to 10 (infra change)
 14:23 — Error rate spiked to 40% on POST /products
 14:31 — /incident triage identified RDS Proxy as likely cause
 14:38 — MaxConnectionsPercent restored to 50 via Console
 14:40 — Error rate returned to 0%
Root cause: RDS Proxy connection limit too low for Lambda concurrency
Impact: 17 minutes, ~280 failed POST /products requests"
```

Claude generates:

```markdown
## Incident Postmortem

Severity: P1
Duration: 14:23 → 14:40 UTC (17 minutes customer-impacting)
Impact: ~280 failed POST /products requests (40% error rate at peak)

### Timeline
| Time  | Event |
|-------|-------|
| 14:15 | RDS Proxy MaxConnectionsPercent reduced 50 → 10 (routine infra task) |
| 14:23 | Error rate spiked: POST /products timing out at 30s Lambda limit |
| 14:31 | /incident triage: RDS Proxy connection limit identified as top hypothesis |
| 14:38 | MaxConnectionsPercent restored to 50 via AWS Console |
| 14:40 | Error rate returned to baseline 0% |

### Root Cause
RDS Proxy was configured to allow a maximum of 10 simultaneous DB connections.
Under normal load, Lambda scales to ~15–20 concurrent invocations.
Each invocation holds a DB connection for the duration of the request.
Connections 11–20 timed out waiting for a slot, causing Lambda to hit its 30s timeout.

### Contributing Factors
- No alerting on RDS Proxy DatabaseConnectionsCurrentlyBorrowed metric
- Infra change made without cross-referencing Lambda concurrency requirements
- No runbook documenting the relationship between RDS Proxy limits and Lambda scaling

### What Went Well
- /incident triage identified root cause within 8 minutes of first alert
- Console fix was reversible and took < 2 minutes to apply
- No data loss — failed requests returned errors, no partial writes

### Action Items
| Item | Owner | Due |
|------|-------|-----|
| Add CloudWatch alarm: RDS Proxy connections > 80% of limit | DevOps | 2026-04-30 |
| Document Lambda concurrency ↔ RDS Proxy sizing in runbook | DevOps | 2026-04-30 |
| Add RDS Proxy MaxConnectionsPercent to CDK stack with comment explaining minimum value | DevOps | 2026-05-07 |
| Add integration test that fails under connection exhaustion | DEV | 2026-05-07 |
```

---

## DevOps Checklist

```
□ /migrate run on every schema change before deployment
□ GitLab CI pipeline reviewed — manual gate on production deploy
□ OIDC role assumption — no long-lived AWS credentials in CI variables
□ CDK stack reviewed — RDS Proxy, Lambda concurrency, IAM least privilege
□ CloudWatch alarms defined: error rate, Lambda timeout, RDS Proxy connections
□ /compliance run before first production deployment
□ Runbook exists: how to rollback Lambda, how to restore RDS Proxy config
□ Postmortem written for every P1/P2 incident
```
