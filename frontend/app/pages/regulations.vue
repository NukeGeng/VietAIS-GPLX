<script setup lang="ts">
type Regulation = { version: string; title: string; summary: string; effectiveFrom: string; source: { title: string; url: string; retrievedAt: string } };
type Blueprint = { version: string; effectiveFrom: string; blueprints: Array<{ licenseClassSlug: string; questionCount: number; durationSeconds: number; passingScore: number; maxCriticalMistakes: number; topicQuestionCounts: Record<string, number> }> };
const { request } = useGplxApi();
const { data } = await useAsyncData("public-regulations", async () => {
  const [regulations, blueprints] = await Promise.all([request<Regulation[]>("/regulations"), request<Blueprint[]>("/exam-blueprints")]);
  return { regulations, blueprints };
}, { default: () => ({ regulations: [], blueprints: [] }) });
const regulation = computed(() => data.value?.regulations?.[0]);
const blueprint = computed(() => data.value?.blueprints?.[0]);
useSeoMeta({ title: "Quy định thi GPLX 2025 — VietAIS", description: () => regulation.value?.summary ?? "Quy định và cấu trúc đề thi GPLX theo hạng bằng." });
useGplxSeo("/regulations", { "@type": "Article", headline: "Quy định thi GPLX", dateModified: regulation.value?.source?.retrievedAt });
</script>

<template>
  <section class="content-width page-section">
    <div class="page-intro"><span class="eyebrow">Regulation / Blueprint</span><h1>Quy định thi GPLX</h1><p>Thông tin phiên bản, ngày hiệu lực và cấu trúc đề thi được công bố cho từng hạng bằng.</p></div>
    <article v-if="regulation" class="surface-card regulation-card"><span class="eyebrow">{{ regulation.version }}</span><h2>{{ regulation.title }}</h2><p>{{ regulation.summary }}</p><div class="source-note"><span>Hiệu lực từ {{ regulation.effectiveFrom }}</span><span>Cập nhật nguồn {{ regulation.source.retrievedAt }}</span><a :href="regulation.source.url" target="_blank" rel="noreferrer">Mở nguồn ↗</a></div></article>
    <div v-if="blueprint" class="feature-grid regulation-grid"><article v-for="rule in blueprint.blueprints" :key="rule.licenseClassSlug" class="surface-card feature-card"><span class="license-code">{{ rule.licenseClassSlug.toUpperCase() }}</span><h2>Hạng {{ rule.licenseClassSlug.toUpperCase() }}</h2><p>{{ rule.questionCount }} câu · {{ Math.round(rule.durationSeconds / 60) }} phút · đạt từ {{ rule.passingScore }} câu.</p><p>Tối đa {{ rule.maxCriticalMistakes }} câu điểm liệt sai.</p><NuxtLink class="text-link" :to="`/practice?licenseClassSlug=${rule.licenseClassSlug}`">Luyện theo hạng →</NuxtLink></article></div>
  </section>
</template>
