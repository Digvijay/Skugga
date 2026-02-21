---
layout: home

hero:
  name: "Skugga"
  text: "Compile-Time Mocking for Native AOT .NET"
  tagline: "Mocking at the Speed of Compilation. Zero Reflection. Zero Overhead."
  image:
    src: /icon.png
    alt: Skugga Logo
  actions:
    - theme: brand
      text: Get Started
      link: /guide/getting-started
    - theme: alt
      text: Architecture
      link: /concepts/architecture
    - theme: alt
      text: View on GitHub
      link: https://github.com/Digvijay/Skugga

features:
  - title: "Native AOT First"
    details: "Built from the ground up for Native AOT. Zero reflection, zero JIT, zero dynamic proxies -- mocks compile to static machine code."
  - title: "Familiar API"
    details: "100% Moq-compatible API surface. Setup, Verify, It.IsAny -- everything you know. Migrate in minutes."
  - title: "Doppelganger (OpenAPI Mocks)"
    details: "Generate mocks from OpenAPI specs at build time. Contract drift = build failure, not production crash."
  - title: "AutoScribe (Self-Writing Tests)"
    details: "Record real service interactions and generate mock setup code automatically. 15 minutes to 30 seconds."
  - title: "Chaos Engineering"
    details: "Industry-first: Built-in fault injection for mocks. Prove your retry logic works before production breaks."
  - title: "Zero-Allocation Testing"
    details: "Industry-first: GC-level assertions to prove your hot paths are truly allocation-free. Catch regressions in CI."
---
