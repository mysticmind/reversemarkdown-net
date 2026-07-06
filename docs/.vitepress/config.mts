import { defineConfig } from 'vitepress'

// Deployed to GitHub Pages at https://mysticmind.github.io/reversemarkdown-net/
export default defineConfig({
  title: 'ReverseMarkdown',
  description: 'A .NET HTML to Markdown converter library.',
  base: '/reversemarkdown-net/',
  lastUpdated: true,
  cleanUrls: true,
  // Architecture Decision Records live under docs/adr but are internal - not site pages.
  srcExclude: ['adr/**'],
  head: [
    ['link', { rel: 'icon', href: '/reversemarkdown-net/logo.png' }],
  ],
  themeConfig: {
    logo: '/logo.png',
    nav: [
      { text: 'Guide', link: '/guide/getting-started' },
      { text: 'Flavors', link: '/flavors/' },
      { text: 'Configuration', link: '/configuration' },
      { text: 'Extending', link: '/extending' },
      { text: 'Migrate from v5', link: '/migration' },
      { text: 'NuGet', link: 'https://www.nuget.org/packages/ReverseMarkdown/' },
    ],
    sidebar: [
      {
        text: 'Guide',
        items: [
          { text: 'Introduction', link: '/guide/introduction' },
          { text: 'Getting Started', link: '/guide/getting-started' },
          { text: 'Supported Frameworks', link: '/guide/supported-frameworks' },
        ],
      },
      {
        text: 'Flavors',
        items: [
          { text: 'Overview', link: '/flavors/' },
          { text: 'GitHub', link: '/flavors/github' },
          { text: 'CommonMark', link: '/flavors/commonmark' },
          { text: 'Slack', link: '/flavors/slack' },
          { text: 'Telegram', link: '/flavors/telegram' },
          { text: 'MultiMarkdown', link: '/flavors/multimarkdown' },
          { text: 'Pandoc', link: '/flavors/pandoc' },
        ],
      },
      {
        text: 'Reference',
        items: [
          { text: 'Configuration', link: '/configuration' },
          { text: 'Extending', link: '/extending' },
          { text: 'Migrate from v5', link: '/migration' },
        ],
      },
    ],
    socialLinks: [
      { icon: 'github', link: 'https://github.com/mysticmind/reversemarkdown-net' },
    ],
    search: { provider: 'local' },
    editLink: {
      pattern: 'https://github.com/mysticmind/reversemarkdown-net/edit/master/docs/:path',
      text: 'Edit this page on GitHub',
    },
    footer: {
      message: 'Released under the MIT License.',
      copyright: 'Copyright © Babu Annamalai',
    },
  },
})
