<script setup lang="ts">
const route = useRoute();
const { request } = useGplxApi();
const id = String(route.params.id);
const { data } = await useAsyncData(
  `result-${id}`,
  () => request<{ view: any }>(`/exams/${id}/result`),
  { default: () => null },
);
const result = computed(() => data.value?.view ?? data.value);
useSeoMeta({
  title: "Kết quả thi thử — VietAIS GPLX",
  robots: "noindex,nofollow",
});
</script>

<template>
  <section class="content-width page-section narrow-content">
    <div v-if="!result" class="empty-state">
      <strong>Chưa có kết quả</strong><NuxtLink to="/">Về trang chủ</NuxtLink>
    </div>
    <article v-else class="result-card surface-card">
      <span class="eyebrow">Kết quả thi thử</span>
      <div :class="['result-status', result.passed ? 'passed' : 'failed']">
        {{ result.passed ? "Đạt" : "Chưa đạt" }}
      </div>
      <div class="result-score">
        <strong>{{ result.score ?? 0 }}</strong
        ><span>/ {{ result.questionIds?.length ?? 0 }} câu đúng</span>
      </div>
      <p>
        {{
          result.passed
            ? "Bạn đã hoàn thành bài thi. Hãy tiếp tục luyện tập để duy trì phong độ."
            : "Hãy xem lại các chủ đề chưa chắc và thi lại khi sẵn sàng."
        }}
      </p>
      <div class="result-actions">
        <NuxtLink class="button button-primary" to="/">Thi lại</NuxtLink
        ><NuxtLink class="button button-quiet" to="/questions"
          >Xem giải thích</NuxtLink
        >
      </div>
    </article>
  </section>
</template>
