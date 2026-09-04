export default defineEventHandler(async (event) => {
  const config = useRuntimeConfig(event);
  const path = event.context.params?.path ?? "";
  const method = event.method.toUpperCase();
  const headers = new Headers();

  for (const name of ["authorization", "content-type", "accept"]) {
    const value = getHeader(event, name);
    if (value) headers.set(name, value);
  }

  try {
    return await $fetch(`${config.apiInternalBase}/${path}`, {
      method,
      headers,
      query: getQuery(event),
      body: method === "GET" || method === "HEAD" ? undefined : await readBody(event),
    });
  } catch (error: any) {
    throw createError({
      statusCode: error?.response?.status ?? 502,
      statusMessage: error?.response?._data?.title ?? "API proxy request failed",
      data: error?.response?._data,
    });
  }
});
