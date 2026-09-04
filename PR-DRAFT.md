# Draft PR — GPLX V1 foundation

## Scope

Implement the first vertical slice of VietAIS GPLX: official normalized question data, public learning/exam flows, admin version management, event-sourced exam attempts, command-side snapshots, inline exam views, async question analytics, SSR/SEO/GEO foundation, and deployable Nginx/Docker configuration.

## Event Storming

### Question bank

`Admin → ImportQuestionBank → QuestionBankVersion → validate/preview → publish/deprecate → public QuestionBank reads`

Question Bank, License Class, Regulation and Blueprint are versioned Marten documents. They are not event-sourced because GOAL limits Event Sourcing to behavior that needs history.

### Exam

`Guest → StartExam → ExamAttempt → ExamStarted`

`Guest → AnswerQuestion/ChangeAnswer → ExamAttempt → QuestionAnswered/AnswerChanged`

`Guest → Flag/UnflagQuestion → ExamAttempt → QuestionFlagged/QuestionUnflagged`

`Guest → SubmitExam → ExamAttempt → ExamSubmitted → ExamScored → QuestionScored`

Inline `ExamAttemptView`/`ExamAttemptSnapshot` provide immediate command feedback. `QuestionPerformanceProjection` aggregates `QuestionScored` across attempt streams asynchronously for analytics.

## Architecture decisions

- Modular monolith in ASP.NET Core/C# with PostgreSQL, Marten and Wolverine.
- Only `ExamAttempt` uses Event Sourcing. Question Bank reference data uses current-state Marten documents.
- `ExamAttemptSnapshot` stores command state and stream version. Commands hydrate snapshot plus delta events; `AppendOptimistic` provides concurrency protection compatible with Marten Quick append mode.
- Exam start pins Question Bank, Exam Blueprint and Regulation versions. Question distribution comes from the published blueprint, not hard-coded endpoint constants.
- Public question/practice reads are limited to published Question Bank versions. Imported question identities are scoped by `bank version + source question id`, so a new bank cannot overwrite documents referenced by an in-progress attempt. The normalized seeder is bootstrap-only and preserves Admin-created versions across restart.
- Exam views are Inline because answer progress and result need immediate consistency. Analytics is an Async `MultiStreamProjection` because it aggregates `QuestionScored` across attempt streams, tolerates eventual consistency, and supports a daemon rebuild.
- Public reads use documents/read models directly. Question list/practice queries use database-side count and pagination; Nginx caches only public read-heavy GET routes.
- Admin authorization uses named permission policies and JWT claims; no business logic checks a role string.

## Verification

- `python3 data/scripts/validate/validate_question_bank.py`
- `dotnet build backend/VietAIS.Gplx.slnx --no-restore`
- `dotnet test backend/VietAIS.Gplx.slnx --no-restore` — 5/5
- `npm run build` in `frontend`
- `npm audit --omit=dev` — 0 vulnerabilities
- NuGet vulnerability check — no vulnerable packages
- Docker Compose syntax — pass with required environment placeholders
- API smoke: health, public question/practice/regulation/blueprint endpoints, admin login/permissions, projection status, B exam start/answer/submit
- Exam interaction smoke: flag/unflag updates the inline attempt view and is exposed in the exam question index and current-question toolbar.
- Exam UI derives its countdown from the pinned `ExpiresAt`; no timer ticks are persisted as events.
- Analytics smoke: projection rebuild returns `202`, preserves the aggregate total, restarts the daemon shard, and a post-rebuild 30-question exam increments analytics exactly once.
- SSR smoke: `/`, `/questions`, `/practice`, `/regulations`, `/licenses/b`, `/admin/login`, `/admin`, `/sitemap.xml`; invalid license returns 404

## Frontend Reference

The read-only reference `/Users/dangvyhao/Documents/NLT_Tranning/front-end/nlt-smart-gas` was inspected before frontend changes. GPLX reuses its compact typography, coral accent, card/button/input states, spacing, and responsive layout patterns. No reference file was changed and no Smart Gas business/API logic was copied.

## Out of Scope

RabbitMQ/Kafka, microservices, Redis, Kubernetes, mobile app, social/account system, payments, AI chatbot/recommendation engine, and Event Sourcing for reference data remain out of scope.

## Risks / Follow-up

- Run browser-based desktop/tablet/mobile visual QA before marking the PR Ready.
- Configure a production TLS certificate and real `GPLX_*` secrets at deployment time; local Compose intentionally requires these values.
- Push branch and open the Draft PR after GitHub credentials are corrected. Current HTTPS token lacks `workflow` scope and current SSH identity lacks repository write permission.
