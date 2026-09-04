# GITRULEBASE.md — VietAIS GPLX

## 1. Branch hierarchy

Luồng branch chính:

```text
main
 ↓
develop
 ↓
module/(fe|be|data)-<short-description>
```

Ví dụ:

```text
module/fe-exam-ui
module/fe-question-page

module/be-exam-attempt
module/be-question-bank
module/be-admin-auth

module/data-question-bank-2025
module/data-exam-blueprint-2026
```

Không commit feature trực tiếp vào `main` hoặc `develop`.

---

## 2. Ý nghĩa branch

### main

- Production/stable branch.
- Chỉ nhận code đã qua `develop` và quy trình release phù hợp.
- Không làm feature trực tiếp trên `main`.

### develop

- Integration branch.
- Nhận module/task hoàn thành từ Draft PR/PR.
- Là baseline để tạo module branch mới.

### module/*

- Branch triển khai một module/task có scope rõ ràng.
- Phải xuất phát từ `develop` mới nhất khi bắt đầu task.

---

## 3. Naming convention

Format:

```text
module/<area>-<short-description>
```

Area hợp lệ hiện tại:

```text
fe
be
data
```

Tên branch:

- chữ thường.
- dùng dấu `-`.
- ngắn nhưng mô tả được scope.
- không dùng tên chung chung như `fix`, `update`, `new-feature`.

Tốt:

```text
module/be-submit-exam
module/fe-exam-result
module/data-regulation-2027
```

Không tốt:

```text
module/be-stuff
module/fe-update
module/data-new
```

---

## 4. Commit discipline

Không commit quá nhiều commit nhỏ vô nghĩa.

Không tạo commit cho từng thay đổi vài dòng nếu chúng cùng một logical unit.

Một commit nên đại diện cho một thay đổi có ý nghĩa và review được.

Ví dụ tốt:

```text
feat(exams): add exam attempt event stream
feat(exams): add submit and scoring flow
feat(exams): add inline result projection

test(exams): cover submit invariants
```

Không tốt:

```text
update
fix
fix again
try
working
final
final2
```

Không nhồi toàn bộ module thành một commit khổng lồ nếu có thể tách thành vài logical commits rõ ràng.

Mục tiêu:

> Ít commit, nhưng mỗi commit có ý nghĩa.

---

## 5. Commit message

Ưu tiên Conventional Commit style:

```text
feat(...): ...
fix(...): ...
refactor(...): ...
test(...): ...
docs(...): ...
chore(...): ...
perf(...): ...
```

Scope nên phản ánh module/domain.

Ví dụ:

```text
feat(question-bank): add version publishing flow
feat(exams): add answer question command
perf(api): cache public question responses
fix(exams): prevent duplicate submit
```

---

## 6. Một module/task hoàn thành → Draft PR

Khi scope module/task đã hoàn thành:

```text
module/*
 ↓
Draft PR
 ↓
develop
```

Không đợi branch chứa quá nhiều module không liên quan mới mở PR.

Một PR nên có scope đủ nhỏ để review rõ ràng.

---

## 7. Draft PR checklist

Trước khi mở Draft PR:

- Sync/rebase/merge `develop` theo convention repo nếu cần.
- Build pass.
- Test liên quan pass.
- Không có file ngoài scope.
- Không có secret/config local bị commit.
- Không có debug code thừa.

Draft PR phải mô tả:

```text
Scope
Event Storming
Architecture decisions
Projection mode nếu có
Verification
Out of Scope
Risks/Follow-up
```

---

## 8. Preview / Review Draft PR

Sau khi mở Draft PR phải preview lại toàn bộ PR trước khi submit:

1. Review Files Changed.
2. Review commit list.
3. Kiểm tra scope creep.
4. Kiểm tra architecture đúng GOAL.md.
5. Kiểm tra workflow đúng WORKFLOW.md.
6. Kiểm tra agent/code không vi phạm AGENTS.md.
7. Chạy lại test/build cần thiết.
8. Sửa các lỗi phát hiện trong Draft PR.
9. Resolve review comments.

Không chuyển Draft PR thành Ready nếu chưa review diff.

---

## 9. Submit PR vào develop

Khi Draft PR đã được preview/review đầy đủ:

```text
Draft PR
 ↓
Ready for Review
 ↓
Submit/Merge vào develop
```

Không merge nếu:

- Test quan trọng fail.
- Build fail.
- Scope chưa hoàn thành.
- Có unresolved critical review comment.
- Có thay đổi architecture ngoài GOAL.

---

## 10. Sau khi merge

Sau khi PR merge vào `develop`:

1. Pull/sync `develop` mới nhất.
2. Xóa local module branch nếu không còn cần.
3. Xóa remote branch nếu không còn cần và policy cho phép.
4. Không tái sử dụng branch cũ cho module/task mới không liên quan.

Task mới → branch mới từ `develop` mới nhất.

---

## 11. Branch cleanup

Phải dọn branch nếu:

- Đã merge và không còn dùng.
- Task bị hủy.
- Branch quá lỗi thời so với `develop`.
- Branch diverge quá xa và việc cứu branch khó/rủi ro hơn tạo branch mới.
- Branch chứa thử nghiệm đã bị thay thế.

Không giữ hàng chục branch chết trong repository.

Nếu branch quá lỗi thời:

```text
đánh giá phần code còn giá trị
→ cherry-pick/reapply phần cần thiết nếu hợp lý
→ tạo branch mới từ develop
→ xóa branch cũ
```

Không cố merge một branch cũ đầy conflict chỉ để giữ lịch sử.

---

## 12. Không dùng branch làm kho lưu trữ

Git branch không phải backup storage.

Nếu code không còn thuộc active work:

- Merge nếu hợp lệ.
- Lưu bằng tag/release nếu thực sự cần.
- Hoặc xóa branch.

Không để branch tồn tại vô thời hạn chỉ để “phòng khi cần”.

---

## 13. Hotfix production

V1 chưa mở flow phức tạp riêng cho hotfix nếu chưa cần.

Nếu production thực tế cần hotfix workflow riêng, phải cập nhật `GITRULEBASE.md` trước khi áp dụng.

Không tự sinh thêm `release/*`, `hotfix/*`, `support/*` chỉ vì GitFlow truyền thống có các branch này.

---

## 14. Rule cuối cùng

Branching phải phục vụ khả năng review và delivery, không phục vụ việc làm Git trông phức tạp.

Giữ flow:

```text
main
  ↑
develop
  ↑
module/(fe|be|data)-...
```

đơn giản cho đến khi dự án thật sự cần thêm quy trình khác.
