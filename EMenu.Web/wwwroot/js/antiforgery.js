window.emenu = window.emenu || {};

window.emenu.getRequestVerificationToken = function () {
  const tokenElement = document.querySelector(
    'meta[name="request-verification-token"]',
  );

  return tokenElement ? tokenElement.content : "";
};

window.emenu.getAntiforgeryHeaders = function (headers) {
  const requestToken = window.emenu.getRequestVerificationToken();
  const nextHeaders = Object.assign({}, headers || {});

  if (requestToken) {
    nextHeaders.RequestVerificationToken = requestToken;
  }

  return nextHeaders;
};
