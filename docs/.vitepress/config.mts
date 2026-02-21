import { defineConfig } from 'vitepress'

export default defineConfig({
    base: '/',
    title: "Skugga",
    description: "Compile-Time Mocking for Native AOT .NET",
    srcExclude: [
        'ALLOCATION_TESTING.md',
        'AOT_COMPATIBILITY_ANALYSIS.md',
        'API_REFERENCE.md',
        'AUTOSCRIBE.md',
        'CHAOS_ENGINEERING.md',
        'DEPENDENCIES.md',
        'DOPPELGANGER.md',
        'EXECUTIVE_SUMMARY.md',
        'README.md',
        'TECHNICAL_SUMMARY.md',
        'TROUBLESHOOTING.md',
    ],
    head: [
        ['link', { rel: 'icon', href: '/icon.png' }]
    ],
    themeConfig: {
        nav: [
            { text: 'Home', link: '/' },
            { text: 'Guide', link: '/guide/getting-started' },
            { text: 'Features', link: '/features/chaos-engineering' },
            { text: 'Concepts', link: '/concepts/architecture' },
            { text: 'Reference', link: '/api/index' }
        ],

        sidebar: [
            {
                text: 'Introduction',
                items: [
                    { text: 'Getting Started', link: '/guide/getting-started' },
                    { text: 'Architecture', link: '/concepts/architecture' },
                    { text: 'Migrating from Moq', link: '/guide/migration' },
                    { text: 'Skugga vs Others', link: '/guide/comparison' },
                    { text: 'Roadmap', link: '/roadmap' }
                ]
            },
            {
                text: 'Core Usage',
                items: [
                    { text: 'Mock Creation', link: '/guide/mock-creation' },
                    { text: 'Setup & Returns', link: '/guide/setup-returns' },
                    { text: 'Verification', link: '/guide/verification' },
                    { text: 'Argument Matchers', link: '/guide/argument-matchers' },
                    { text: 'Strict Mocks', link: '/guide/strict-mocks' },
                    { text: 'Setup Sequences', link: '/guide/setup-sequences' },
                    { text: 'Protected Members', link: '/guide/protected-members' },
                    { text: 'Ref & Out Parameters', link: '/guide/ref-out-params' }
                ]
            },
            {
                text: 'Exclusive Features',
                items: [
                    { text: 'Doppelgänger (OpenAPI)', link: '/features/doppelganger' },
                    { text: 'AutoScribe', link: '/features/autoscribe' },
                    { text: 'Chaos Engineering', link: '/features/chaos-engineering' },
                    { text: 'Zero-Allocation Testing', link: '/features/allocation-testing' }
                ]
            },
            {
                text: 'Concepts',
                items: [
                    { text: 'How It Works', link: '/concepts/architecture' },
                    { text: 'The Reflection Wall', link: '/concepts/reflection-wall' },
                    { text: 'Technical Summary', link: '/concepts/technical-summary' },
                    { text: 'Executive Summary', link: '/concepts/executive-summary' }
                ]
            },
            {
                text: 'Benchmarks',
                items: [
                    { text: 'Performance', link: '/benchmarks/overview' },
                    { text: 'Cloud & AOT', link: '/benchmarks/cloud-aot' }
                ]
            },
            {
                text: 'API Reference',
                items: [
                    { text: 'API Overview', link: '/api/index' },
                    { text: 'Troubleshooting', link: '/api/troubleshooting' }
                ]
            },
            {
                text: 'Resources',
                items: [
                    { text: 'Changelog', link: '/changelog' },
                    { text: 'Contributing', link: '/contributing' },
                    { text: 'Security', link: '/security' }
                ]
            }
        ],

        socialLinks: [
            { icon: 'github', link: 'https://github.com/Digvijay/Skugga' }
        ],

        footer: {
            message: 'Released under the MIT License.',
            copyright: 'Copyright 2026 Digvijay'
        }
    }
})
