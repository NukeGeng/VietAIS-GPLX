<script setup lang="ts">
type VersionedItem = {
  id: string;
  version: string;
  status: string;
  effectiveFrom: string;
};

const { request } = useGplxApi();
const banks = ref<VersionedItem[]>([]);
const regulations = ref<VersionedItem[]>([]);
const blueprints = ref<VersionedItem[]>([]);
const licenses = ref<any[]>([]);
const analytics = ref<any[]>([]);
const projectionStatus = ref<any[]>([]);
const preview = ref<any | null>(null);
const error = ref("");
const loading = ref(true);
const actionId = ref("");
const importPayload = ref("");
const licensePayload = ref("");
const regulationPayload = ref("");
const blueprintPayload = ref("");
const questionVersionId = ref("");
const questionId = ref("");
const questionPayload = ref("");
const importing = ref(false);

useSeoMeta({ title: "Admin — VietAIS GPLX", robots: "noindex,nofollow" });

const authHeaders = () => ({
  Authorization: "Bearer " + (localStorage.getItem("gplx_admin_token") ?? ""),
});

async function loadAdmin() {
  loading.value = true;
  error.value = "";
  try {
    const headers = authHeaders();
    const [bankData, regulationData, blueprintData, licenseData, performanceData, daemonData] =
      await Promise.all([
        request<VersionedItem[]>("/admin/question-banks", { headers }),
        request<VersionedItem[]>("/admin/regulations", { headers }),
        request<VersionedItem[]>("/admin/exam-blueprints", { headers }),
        request<any[]>("/admin/license-classes", { headers }),
        request<any[]>("/admin/analytics/question-performance", { headers }),
        request<any[]>("/admin/projection/status", { headers }),
      ]);
    banks.value = bankData;
    regulations.value = regulationData;
    blueprints.value = blueprintData;
    licenses.value = licenseData;
    analytics.value = performanceData;
    projectionStatus.value = daemonData;
  } catch {
    error.value = "Phiên đăng nhập hết hạn hoặc chưa có quyền truy cập.";
  } finally {
    loading.value = false;
  }
}

async function saveJson(
  path: string,
  value: { value: string },
  label: string,
  method: "POST" | "PUT" = "POST",
) {
  if (actionId.value || !value.value.trim()) return;
  actionId.value = label;
  error.value = "";
  try {
    await request(path, {
      method,
      headers: authHeaders(),
      body: JSON.parse(value.value),
    });
    value.value = "";
    await loadAdmin();
  } catch {
    error.value = `Không thể lưu ${label}. Kiểm tra JSON và quyền quản trị.`;
  } finally {
    actionId.value = "";
  }
}

async function showPreview(bank: VersionedItem) {
  try {
    preview.value = await request("/admin/question-banks/" + bank.id + "/preview", {
      headers: authHeaders(),
    });
  } catch {
    error.value = "Không tải được bản preview question bank.";
  }
}

async function publish(
  kind: "question-banks" | "regulations" | "exam-blueprints",
  item: VersionedItem,
) {
  if (actionId.value) return;
  actionId.value = item.id;
  error.value = "";
  try {
    await request("/admin/" + kind + "/" + item.id + "/publish", {
      method: "POST",
      headers: authHeaders(),
    });
    await loadAdmin();
  } catch {
    error.value = "Không thể publish phiên bản. Hãy kiểm tra validation.";
  } finally {
    actionId.value = "";
  }
}

async function importQuestionBank() {
  if (importing.value || !importPayload.value.trim()) return;
  importing.value = true;
  error.value = "";
  try {
    const payload = JSON.parse(importPayload.value);
    await request("/admin/question-banks/import", {
      method: "POST",
      headers: authHeaders(),
      body: payload,
    });
    importPayload.value = "";
    await loadAdmin();
  } catch {
    error.value = "Không thể import. Kiểm tra JSON, source và các trường câu hỏi.";
  } finally {
    importing.value = false;
  }
}

async function editQuestion() {
  await saveJson(
    `/admin/question-banks/${questionVersionId.value}/questions/${questionId.value}`,
    questionPayload,
    "câu hỏi",
    "PUT",
  );
}

onMounted(loadAdmin);
</script>

<template>
  <section class="content-width page-section">
    <div class="page-intro">
      <span class="eyebrow">Admin portal</span>
      <h1>Quản trị dữ liệu</h1>
      <p>
        Preview, publish và theo dõi các phiên bản Question Bank, Regulation,
        Exam Blueprint cùng analytics bất đồng bộ.
      </p>
    </div>

    <p v-if="error" class="inline-error" role="alert">
      {{ error }} <NuxtLink to="/admin/login">Đăng nhập lại</NuxtLink>
    </p>
    <div v-if="loading" class="empty-state"><strong>Đang tải dữ liệu…</strong></div>

    <div v-else class="admin-grid">
      <article class="surface-card admin-panel admin-import">
        <div class="section-heading compact-heading">
          <div><span class="eyebrow">Version management</span><h2>Import Question Bank</h2></div>
        </div>
        <p class="admin-help">Dán normalized bundle JSON để validate và tạo version mới ở trạng thái Validated.</p>
        <textarea v-model="importPayload" class="admin-json" rows="5" placeholder='{"version":"...","effectiveFrom":"2025-06-01","licenseClassSlugs":["b"],"questions":[],"source":{}}' />
        <button class="button button-primary" :disabled="importing || !importPayload.trim()" @click="importQuestionBank">{{ importing ? "Đang import…" : "Validate & import" }}</button>
      </article>
      <article class="surface-card admin-panel admin-import">
        <div class="section-heading compact-heading">
          <div><span class="eyebrow">Reference data</span><h2>Quản lý version</h2></div>
          <span class="section-count">{{ licenses.length }} hạng</span>
        </div>
        <p class="admin-help">Lưu bản nháp License Class, Regulation hoặc Exam Blueprint bằng normalized JSON; chỉ bản đã validate mới được publish.</p>
        <textarea v-model="licensePayload" class="admin-json" rows="3" placeholder='License: {"slug":"b","code":"B","name":"Ô tô","description":"...","source":{...}}' />
        <button class="button button-quiet" :disabled="!licensePayload.trim() || !!actionId" @click="saveJson('/admin/license-classes', licensePayload, 'license class')">{{ actionId === "license class" ? "Đang lưu…" : "Lưu license class" }}</button>
        <textarea v-model="regulationPayload" class="admin-json" rows="3" placeholder='Regulation: {"version":"...","title":"...","summary":"...","effectiveFrom":"2025-06-01","source":{...}}' />
        <button class="button button-quiet" :disabled="!regulationPayload.trim() || !!actionId" @click="saveJson('/admin/regulations', regulationPayload, 'regulation')">{{ actionId === "regulation" ? "Đang lưu…" : "Lưu regulation" }}</button>
        <textarea v-model="blueprintPayload" class="admin-json" rows="3" placeholder='Blueprint: {"version":"...","effectiveFrom":"2025-06-01","blueprints":[...],"source":{...}}' />
        <button class="button button-quiet" :disabled="!blueprintPayload.trim() || !!actionId" @click="saveJson('/admin/exam-blueprints', blueprintPayload, 'exam blueprint')">{{ actionId === "exam blueprint" ? "Đang lưu…" : "Lưu blueprint" }}</button>
      </article>
      <article class="surface-card admin-panel">
        <div class="section-heading compact-heading">
          <div><span class="eyebrow">Reference data</span><h2>Question Bank</h2></div>
          <span class="section-count">{{ banks.length }} version</span>
        </div>
        <div class="admin-list">
          <div v-for="bank in banks" :key="bank.id" class="admin-row">
            <span class="status-badge" :class="'status-' + bank.status.toLowerCase()">{{ bank.status }}</span>
            <strong>{{ bank.version }}</strong><small>{{ bank.effectiveFrom }}</small>
            <button class="text-link-button" @click="showPreview(bank)">Preview</button>
            <button
              v-if="bank.status !== 'Published'"
              class="text-link-button"
              :disabled="actionId === bank.id"
              @click="publish('question-banks', bank)"
            >{{ actionId === bank.id ? "…" : "Publish" }}</button>
          </div>
        </div>
      </article>

      <article class="surface-card admin-panel">
        <div class="section-heading compact-heading">
          <div><span class="eyebrow">Exam rules</span><h2>Regulation & Blueprint</h2></div>
        </div>
        <div class="admin-list">
          <div v-for="item in regulations" :key="item.id" class="admin-row">
            <span class="status-badge" :class="'status-' + item.status.toLowerCase()">{{ item.status }}</span>
            <strong>Regulation {{ item.version }}</strong><small>{{ item.effectiveFrom }}</small>
            <button v-if="item.status !== 'Published'" class="text-link-button" @click="publish('regulations', item)">Publish</button>
          </div>
          <div v-for="item in blueprints" :key="item.id" class="admin-row">
            <span class="status-badge" :class="'status-' + item.status.toLowerCase()">{{ item.status }}</span>
            <strong>Blueprint {{ item.version }}</strong><small>{{ item.effectiveFrom }}</small>
            <button v-if="item.status !== 'Published'" class="text-link-button" @click="publish('exam-blueprints', item)">Publish</button>
          </div>
        </div>
      </article>

      <article v-if="preview" class="surface-card admin-panel admin-preview">
        <div class="section-heading compact-heading">
          <div><span class="eyebrow">Read-only preview</span><h2>{{ preview.version.version }}</h2></div>
          <button class="text-link-button" @click="preview = null">Đóng</button>
        </div>
        <p :class="preview.validationErrors?.length ? 'inline-error' : 'inline-success'">
          {{ preview.questions?.length ?? 0 }} câu hỏi ·
          {{ preview.validationErrors?.length ? preview.validationErrors.length + " lỗi" : "Validation OK" }}
        </p>
        <div class="preview-question" v-for="question in preview.questions?.slice(0, 5)" :key="question.id">
          <strong>{{ question.slug }}<small>{{ question.id }}</small></strong><span>{{ question.text }}</span>
        </div>
        <p class="admin-help">Để sửa một câu trong version chưa publish, nhập version ID, question ID và normalized question JSON:</p>
        <input v-model="questionVersionId" class="admin-text-input" placeholder="Question bank version ID" />
        <input v-model="questionId" class="admin-text-input" placeholder="Question ID" />
        <textarea v-model="questionPayload" class="admin-json" rows="5" placeholder='{"id":"...","slug":"...","licenseClassSlugs":["b"],"topic":"...","text":"...","options":[...],"correctOptionId":"a","isCritical":false,"explanation":"...","memoryTip":"..."}' />
        <button class="button button-quiet" :disabled="!questionVersionId || !questionId || !questionPayload.trim() || !!actionId" @click="editQuestion">{{ actionId === "câu hỏi" ? "Đang lưu…" : "Lưu câu hỏi" }}</button>
      </article>

      <article class="surface-card admin-panel">
        <div class="section-heading compact-heading">
          <div><span class="eyebrow">Async learning</span><h2>Question performance</h2></div>
          <span class="section-count">{{ analytics.length }} rows</span>
        </div>
        <div v-if="analytics.length" class="admin-list">
          <div v-for="item in analytics.slice(0, 5)" :key="item.id" class="admin-row analytics-row">
            <span class="status-badge status-draft">{{ Math.round(item.accuracy * 100) }}% đúng</span>
            <strong>{{ item.topic || "Question" }}</strong><small>{{ item.attempts }} attempts</small>
          </div>
        </div>
        <div v-else class="empty-state compact-empty"><strong>Chưa có dữ liệu chấm điểm</strong></div>
      </article>

      <article class="surface-card admin-panel">
        <div class="section-heading compact-heading">
          <div><span class="eyebrow">Marten daemon</span><h2>Projection status</h2></div>
        </div>
        <div v-for="item in projectionStatus" :key="item.storeUri" class="daemon-status">
          <span class="status-dot" :class="{ healthy: item.isRunning && !item.isHighWaterStale }" />
          <strong>{{ item.storeUri }}</strong>
          <span>{{ item.isRunning && !item.isHighWaterStale ? "Running" : "Needs attention" }}</span>
        </div>
      </article>
    </div>
  </section>
</template>
