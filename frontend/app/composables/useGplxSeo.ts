export function useGplxSeo(path: string, schema?: Record<string, unknown>) {
  const config = useRuntimeConfig();
  const requestUrl = useRequestURL();
  const configuredSiteUrl = String(config.public.siteUrl || requestUrl.origin).replace(/\/$/, "");
  const canonical = new URL(path, configuredSiteUrl).toString();
  const structuredData = schema
    ? JSON.stringify({ "@context": "https://schema.org", ...schema })
    : undefined;

  useHead({
    link: [{ rel: "canonical", href: canonical }],
    script: structuredData
      ? [{ type: "application/ld+json", children: structuredData }]
      : [],
  });
}
