<script setup lang="ts">
import { demoLicenses } from "~/data/demo";

const route = useRoute();
const { request } = useGplxApi();
const { data: licenseData } = await useAsyncData(
  "license-" + route.params.slug,
  () => request<any[]>("/licenses"),
  { default: () => [] },
);
const license = computed(
  () =>
    licenseData.value?.find((item) => item.slug === route.params.slug) ??
    demoLicenses.find((item) => item.slug === route.params.slug) ??
    demoLicenses[0],
);
useSeoMeta({
  title: () => license.value.name + " — VietAIS GPLX",
  description: () => license.value.description,
});
useGplxSeo("/licenses/" + route.params.slug, {
  "@type": "Article",
  headline: "Ôn thi " + license.value.name,
  description: license.value.description,
  about: "Giấy phép lái xe Việt Nam",
});
</script>

<template>
  <section class="content-width page-section">
    <div class="page-intro">
      <span class="eyebrow">License class / {{ license.code }}</span>
      <h1>Ôn thi {{ license.name }}</h1>
      <p>{{ license.description }}</p>
    </div>
    <div class="feature-grid">
      <article class="surface-card feature-card">
        <span class="feature-icon">◌</span>
        <h2>Luyện theo chủ đề</h2>
        <p>Chọn nhóm câu hỏi và xem giải thích ngay sau mỗi câu.</p>
        <NuxtLink class="text-link" to="/questions"
          >Mở question bank →</NuxtLink
        >
      </article>
      <article class="surface-card feature-card">
        <span class="feature-icon">◈</span>
        <h2>Thi thử toàn bài</h2>
        <p>Bài thi pin theo bộ dữ liệu và blueprint đã publish.</p>
        <NuxtLink
          class="button button-primary"
          :to="`/?license=${license.slug}#license-classes`"
          >Bắt đầu thi thử</NuxtLink
        >
      </article>
    </div>
    <div v-if="license.source" class="source-note">
      <span>Nguồn: {{ license.source.title }}</span>
      <span v-if="license.source.effectiveFrom"
        >Hiệu lực từ {{ license.source.effectiveFrom }}</span
      >
      <a :href="license.source.url" target="_blank" rel="noreferrer"
        >Mở nguồn ↗</a
      >
    </div>
  </section>
</template>
