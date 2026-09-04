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
const analytics = ref<any[]>([]);
const projectionStatus = ref<any[]>([]);
const preview = ref<any | null>(null);
const error = ref("");
const loading = ref(true);
const actionId = ref("");

useSeoMeta({ title: "Admin — VietAIS GPLX", robots: "noindex,nofollow" });

const authHeaders = () => ({
  Authorization: "Bearer " + (localStorage.getItem("gplx_admin_token") ?? ""),
});

async function loadAdmin() {
  loading.value = true;
  error.value = "";
  try {
    const headers = authHeaders();
    const [bankData, regulationData, blueprintData, performanceData, daemonData] =
      await Promise.all([
        request<VersionedItem[]>("/admin/question-banks", { headers }),
        request<VersionedItem[]>("/admin/regulations", { headers }),
        request<VersionedItem[]>("/admin/exam-blueprints", { headers }),
        request<any[]>("/admin/analytics/question-performance", { headers }),
        request<any[]>("/admin/projection/status", { headers }),
      ]);
    banks.value = bankData;
    regulations.value = regulationData;
    blueprints.value = blueprintData;
    analytics.value = performanceData;
    projectionStatus.value = daemonData;
  } catch {
    error.value = "Phiên đăng nhập hết hạn hoặc chưa có quyền truy cập.";
  } finally {
    loading.value = false;
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
          <strong>{{ question.slug }}</strong><span>{{ question.text }}</span>
        </div>
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
