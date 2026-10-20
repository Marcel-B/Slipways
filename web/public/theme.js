// Sets the theme before the first paint; src/components/theme.ts keeps it up to date.
try {
  var theme = localStorage.getItem('slipways-theme')
} catch {}
if (theme !== 'light' && theme !== 'dark')
  theme = window.matchMedia && matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'
document.documentElement.dataset.theme = theme
