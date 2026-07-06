import { defineConfig } from 'vitepress'
import { fileURLToPath, URL } from 'node:url'
import { regionSnippetPlugin } from '@radarleaf/markdown-it-region-snippets'

// Repo root, so the snippet plugin can scan the samples project.
const rootDir = fileURLToPath(new URL('../..', import.meta.url))

// Deployed to GitHub Pages at https://mysticmind.github.io/reversemarkdown-net/
export default defineConfig({
  title: 'ReverseMarkdown',
  description: 'A .NET HTML to Markdown converter library.',
  base: '/reversemarkdown-net/',
  lastUpdated: true,
  cleanUrls: true,
  // Architecture Decision Records live under docs/adr but are internal - not site pages.
  srcExclude: ['adr/**'],
  markdown: {
    // Expand `snippet: sample_*` markers from #region blocks in samples/*.cs so the docs
    // always show real, compiled source. https://www.npmjs.com/package/@radarleaf/markdown-it-region-snippets
    config(md) {
      md.use(regionSnippetPlugin, {
        rootDir,
        dirs: ['samples'],
        include: /^sample_/,
        syntax: 'snippet-colon',
        // Emit an anchor + "source" link under each snippet, pointing at the sample file on GitHub.
        urlPrefix: 'https://github.com/mysticmind/reversemarkdown-net/blob/master',
      })
    },
  },
  head: [
    ['link', { rel: 'icon', href: '/reversemarkdown-net/logo.png' }],
  ],
  themeConfig: {
    logo: '/logo.png',
    nav: [
      {
        // Version switcher. This site is the latest (v6) at the root; v5 is deployed
        // under the /v5/ sub-path (built from the 5.x branch by the deploy workflow).
        text: 'v6.x',
        items: [
          { text: 'v6.x (latest)', link: '/' },
          { text: 'v5.x', link: 'https://mysticmind.github.io/reversemarkdown-net/v5/' },
        ],
      },
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
          { text: 'Performance', link: '/guide/performance' },
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
