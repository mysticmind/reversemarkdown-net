import { defineConfig } from 'vitepress'

// v5 documentation. Deployed to GitHub Pages under the /v5/ sub-path of the main site:
// https://mysticmind.github.io/reversemarkdown-net/v5/  (the root serves the latest, v6).
const LATEST = 'https://mysticmind.github.io/reversemarkdown-net/'

export default defineConfig({
  title: 'ReverseMarkdown v5',
  description: 'A .NET HTML to Markdown converter library (v5).',
  base: '/reversemarkdown-net/v5/',
  lastUpdated: true,
  cleanUrls: true,
  head: [
    ['link', { rel: 'icon', href: '/reversemarkdown-net/v5/logo.png' }],
  ],
  themeConfig: {
    logo: '/logo.png',
    nav: [
      {
        // Version switcher. This is the v5 site; v6 (latest) lives at the site root.
        text: 'v5.x',
        items: [
          { text: 'v6.x (latest)', link: LATEST },
          { text: 'v5.x (current)', link: '/' },
        ],
      },
      { text: 'Guide', link: '/guide/getting-started' },
      { text: 'Flavors', link: '/flavors/' },
      { text: 'Configuration', link: '/configuration' },
      { text: 'Extending', link: '/extending' },
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
          { text: 'Slack', link: '/flavors/slack' },
          { text: 'Telegram', link: '/flavors/telegram' },
          { text: 'CommonMark', link: '/flavors/commonmark' },
        ],
      },
      {
        text: 'Reference',
        items: [
          { text: 'Configuration', link: '/configuration' },
          { text: 'Extending', link: '/extending' },
        ],
      },
    ],
    socialLinks: [
      { icon: 'github', link: 'https://github.com/mysticmind/reversemarkdown-net' },
    ],
    search: { provider: 'local' },
    editLink: {
      pattern: 'https://github.com/mysticmind/reversemarkdown-net/edit/5.x/docs/:path',
      text: 'Edit this page on GitHub',
    },
    footer: {
      message: 'Released under the MIT License.',
      copyright: 'Copyright © Babu Annamalai',
    },
  },
})
