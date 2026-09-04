function escapeXml(value: string) {
  return value
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&apos;");
}

export default defineEventHandler(async (event) => {
  const config = useRuntimeConfig(event);
  const origin = getRequestURL(event).origin;
  const [licenses, firstQuestionPage] = await Promise.all([
    $fetch<Array<{ slug: string }>>(config.apiInternalBase + "/licenses"),
    $fetch<{ items: Array<{ id: string }>; total: number }>(
      config.apiInternalBase + "/questions?pageSize=100",
    ),
  ]);
  const questionPages = await Promise.all(
    Array.from({ length: Math.max(0, Math.ceil((firstQuestionPage.total ?? 0) / 100) - 1) }, (_, index) =>
      $fetch<{ items: Array<{ id: string }> }>(
        config.apiInternalBase + "/questions?page=" + (index + 2) + "&pageSize=100",
      ),
    ),
  );
  const questions = [
    ...(firstQuestionPage.items ?? []),
    ...questionPages.flatMap((page) => page.items ?? []),
  ];
  const paths = [
    "/",
    "/questions",
    ...licenses.map((license) => "/licenses/" + license.slug),
    ...questions.map((question) => "/questions/" + question.id),
  ];
  const urls = paths
    .map(
      (path) =>
        "  <url><loc>" +
        escapeXml(new URL(path, origin).toString()) +
        "</loc></url>",
    )
    .join("\n");

  setHeader(event, "content-type", "application/xml; charset=utf-8");
  return (
    '<?xml version="1.0" encoding="UTF-8"?>\n' +
    '<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">\n' +
    urls +
    "\n</urlset>\n"
  );
});
