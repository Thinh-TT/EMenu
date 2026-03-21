// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

window.emenu = window.emenu || {};

window.emenu.formatCurrency = function (value) {
  const amount = Number(value || 0);

  return new Intl.NumberFormat("vi-VN").format(amount) + " đ";
};
