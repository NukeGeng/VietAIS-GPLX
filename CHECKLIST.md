# VietAIS GPLX — V1 completion checklist

Ngày kiểm tra: 2026-09-04

## Definition of Done

- [x] Official data được normalize, validate và seed/import. Bundle hiện có 600 câu, 60 câu điểm liệt; validator pass; API development seed đã ghi dữ liệu vào Marten.
- [x] Public question/practice reads chỉ expose câu thuộc Question Bank đã publish; import version mới không ghi đè identity câu hỏi của version cũ.
- [x] Guest chọn hạng bằng và thi thử end-to-end. Smoke test B chạy `start → 30 answers → submit`: 30/30, 0 critical mistake, `passed=true`.
- [x] `ExamAttempt` là Event-Sourced Aggregate với các event start/answer/change/flag/unflag/submit/score/question-score.
- [x] Snapshot dùng cho command-side: loader đọc `ExamAttemptSnapshot` rồi chỉ fetch delta events; snapshot mới nhất của smoke test ở stream version 63, có 30 question IDs và 30 answers.
- [x] Inline read models `ExamAttemptView` và `ExamAttemptSnapshot` được lưu cùng transaction command.
- [x] Async `QuestionPerformanceProjection` chạy qua Marten Async Daemon; projection status runtime `running=true`, `stale=false`, endpoint rebuild trả `202`, rebuild không đổi tổng analytics `485`, và bài thi sau rebuild tăng đúng `+30` lượt chấm.
- [x] Wolverine xử lý command và transaction với Marten; không thêm external broker.
- [x] Admin quản lý version: import/validate/preview/publish/deprecate Question Bank, sửa question draft, quản lý License Class, lưu/publish Regulation và Exam Blueprint.
- [x] Authorization dùng permission policies; admin token smoke test có 13 permissions, endpoint protected không token trả `401`.
- [x] SSR/SEO/GEO: canonical, dynamic metadata, JSON-LD, semantic public pages, `/robots.txt`, dynamic sitemap. Sitemap smoke test có 606 URL; invalid license trả `404`.
- [x] Nginx có reverse proxy, public GET cache, rate limit, gzip và security headers; `docker compose config` pass.
- [x] Backend giữ stateless request model; timer dùng `StartedAt`/`ExpiresAt`, không lưu tick event.
- [x] Verification pass: data validator, `dotnet build`, `dotnet test` (5/5), Nuxt production build, `npm audit --omit=dev` (0 vulnerability), NuGet vulnerability check (none).
- [x] Không thêm microservice, broker, Redis, Kubernetes, generic repository hoặc feature ngoài GOAL.

## Frontend reference review

- [x] Đã inspect source read-only tại `/Users/dangvyhao/Documents/NLT_Tranning/front-end/nlt-smart-gas`.
- [x] GPLX reuse visual language: compact Inter/system typography, coral accent, card/button/input states, spacing và responsive grid.
- [x] Không sửa hoặc commit source reference; không copy Smart Gas business logic/API.
- [x] Route-level SSR smoke test pass trên desktop-oriented HTML responses; mobile/tablet CSS breakpoints đã có trong stylesheet.
- [ ] Browser screenshot/manual interaction QA chưa chạy được trong môi trường hiện tại vì in-app browser không khả dụng. Cần chạy lại trước khi chuyển Draft PR sang Ready.

## Git / PR state

- [x] `main` và `develop` đã được tạo, remote HTTPS đã push baseline commit `309f82b`.
- [x] Implementation nằm trên branch `module/be-foundation`; working tree cần được commit trước khi mở PR.
- [ ] Push branch/Draft PR còn chờ quyền GitHub: HTTPS OAuth token thiếu scope `workflow`; SSH key hiện authenticate thành user `NukeLitee` và không có quyền repo `NukeGeng/VietAIS-GPLX`.

## Known verification limits

- Docker daemon chưa chạy trong môi trường kiểm tra nên chưa thực hiện full container build/up.
- Nginx binary không cài trong máy nên chưa chạy `nginx -t`; config syntax/Compose wiring đã được kiểm tra qua file và `docker compose config`.
