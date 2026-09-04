export default defineNuxtConfig({
  compatibilityDate: "2025-07-15",
  future: { compatibilityVersion: 4 },
  devtools: { enabled: false },
  ssr: true,
  css: ["~/assets/css/main.css"],
  runtimeConfig: {
    apiInternalBase:
      process.env.NUXT_API_INTERNAL_BASE || "http://localhost:5080/api",
    public: {
      apiBase: process.env.NUXT_PUBLIC_API_BASE || "/api",
      siteUrl: process.env.NUXT_PUBLIC_SITE_URL || "http://localhost:3000",
    },
  },
  app: {
    head: {
      htmlAttrs: { lang: "vi" },
      meta: [
        { name: "theme-color", content: "#fbfbfb" },
        { name: "robots", content: "index,follow" },
      ],
      link: [{ rel: "icon", type: "image/svg+xml", href: "/favicon.svg" }],
    },
  },
});
