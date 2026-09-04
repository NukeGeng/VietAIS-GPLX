<script setup lang="ts">
useSeoMeta({
  title: "VietAIS GPLX — Ôn thi giấy phép lái xe",
  description:
    "Ôn luyện câu hỏi, biển báo và thi thử GPLX theo hạng bằng với dữ liệu có phiên bản rõ ràng.",
});
useGplxSeo("/", {
  "@type": "WebSite",
  name: "VietAIS GPLX",
  description: "Nền tảng ôn thi giấy phép lái xe theo dữ liệu có nguồn.",
});

const { request } = useGplxApi();
type LicenseClass = {
  slug: string;
  code: string;
  name: string;
  description: string;
};
const { data: licenseData, error: licenseError } = await useAsyncData(
  "licenses",
  () => request<LicenseClass[]>("/licenses"),
  {
    default: () => [],
  },
);
const licenses = computed(() => licenseData.value ?? []);
const startingSlug = ref<string | null>(null);
const startError = ref("");

async function startExam(slug: string) {
  startingSlug.value = slug;
  startError.value = "";
  try {
    const response = await request<{ view: { id: string } }>("/exams", {
      method: "POST",
      body: { licenseClassSlug: slug },
    });
    await navigateTo(`/exam/${response.view.id}`);
  } catch {
    startError.value = "Chưa kết nối được máy chủ. Hãy bật API và thử lại.";
  } finally {
    startingSlug.value = null;
  }
}
</script>

<template>
  <div>
    <section class="hero-section content-width">
      <div class="hero-copy">
        <span class="eyebrow"
          ><span class="status-dot" /> Ôn thi rõ ràng, học đúng trọng tâm</span
        >
        <h1>Vững lý thuyết.<br /><em>Chắc tay lái.</em></h1>
        <p class="hero-lead">
          Luyện thi GPLX với ngân hàng câu hỏi có phiên bản, giải thích dễ hiểu
          và bài thi thử bám theo cấu trúc đã công bố.
        </p>
        <div class="hero-actions">
          <a class="button button-primary" href="#license-classes"
            >Chọn hạng bằng <span>↓</span></a
          >
          <NuxtLink class="button button-quiet" to="/questions"
            >Xem ngân hàng câu hỏi <span>→</span></NuxtLink
          >
        </div>
      </div>
      <div class="hero-card" aria-label="Tóm tắt tính năng">
        <div class="hero-card-top">
          <span class="card-kicker">GPLX / V1</span
          ><span class="signal-bars"><i /><i /><i /></span>
        </div>
        <div class="hero-illustration">
          <span class="road-line" /><span
            class="road-line road-line-two"
          /><span class="steering-wheel">◎</span>
        </div>
        <div class="hero-card-bottom">
          <strong>Học theo dữ liệu có nguồn</strong
          ><span>Versioned · Reviewed · Useful</span>
        </div>
      </div>
    </section>

    <section id="license-classes" class="content-width section-block">
      <div class="section-heading">
        <div>
          <span class="eyebrow">Bắt đầu từ đây</span>
          <h2>Chọn hạng bằng của bạn</h2>
        </div>
        <span class="section-count">{{ licenses.length }} hạng khả dụng</span>
      </div>
      <p v-if="startError" class="inline-error" role="alert">
        {{ startError }}
      </p>
      <div v-if="licenseError" class="empty-state" role="alert">
        <strong>Không tải được danh sách hạng bằng</strong
        ><span>Hãy kiểm tra kết nối máy chủ và thử lại.</span>
      </div>
      <div v-else-if="!licenses.length" class="empty-state">
        <strong>Chưa có hạng bằng khả dụng</strong
        ><span>Dữ liệu đang được cập nhật.</span>
      </div>
      <div class="license-grid">
        <article
          v-for="license in licenses"
          :key="license.slug"
          class="license-card"
        >
          <div class="license-card-top">
            <span class="license-code">{{ license.code }}</span
            ><span class="card-arrow">↗</span>
          </div>
          <h3>{{ license.name }}</h3>
          <p>{{ license.description }}</p>
          <button
            class="card-action"
            :disabled="startingSlug === license.slug"
            @click="startExam(license.slug)"
          >
            {{ startingSlug === license.slug ? "Đang mở…" : "Thi thử ngay" }}
            <span>→</span>
          </button>
        </article>
      </div>
    </section>

    <section class="content-width value-strip" aria-label="Điểm nổi bật">
      <div>
        <span class="value-number">01</span
        ><strong>Dữ liệu có phiên bản</strong>
        <p>Biết mình đang học theo bộ câu hỏi nào.</p>
      </div>
      <div>
        <span class="value-number">02</span><strong>Giải thích ngắn gọn</strong>
        <p>Hiểu lý do thay vì chỉ nhớ đáp án.</p>
      </div>
      <div>
        <span class="value-number">03</span><strong>Thi thử có tiến độ</strong>
        <p>Làm bài trên desktop và mobile.</p>
      </div>
    </section>
  </div>
</template>
