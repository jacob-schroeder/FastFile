# FastFile.cs

> A modern, research-driven effort focused on understanding and rebuilding the internal fastfile and asset systems of Call of Duty: Modern Warfare 2.

---

## Overview

This project exists to push forward the preservation, documentation, and understanding of the internal data structures, asset pipelines, and loading systems used by *Modern Warfare 2* on the PlayStation 3 platform (currently).

Built from extensive reverse engineering research and publicly available findings documented throughout the Call of Duty research community, this repository aims to provide a modern foundation for exploring fastfiles, zone formats, streaming systems, script assets, and engine-level serialization logic.

Primary research references and technical findings are based on the collective information available through the Call of Duty research wiki:

- https://codresearch.dev/index.php/Main_Page

---

# Current Scope

At this stage, development is focused exclusively on:

- **Call of Duty: Modern Warfare 2**
- **PlayStation 3 platform**
- Fastfile parsing and serialization
- Zone asset loading structures
- Pointer resolution systems
- Stream block reconstruction
- Script string handling
- Engine memory layout research
- Asset reading (all)

Future expansion into additional titles or platforms may occur as the project matures.

---

# Goals

This repository is being built with several core principles in mind:

## Modern Architecture

The project prioritizes:

- Clean abstractions
- Readable code
- Maintainable systems
- Explicit parsing logic
- Minimal legacy-style technical debt

The objective is not simply to recreate old tooling, but to establish a modern codebase that is easier to understand, extend, and contribute to.

---

## Accuracy Through Research

A major focus of the project is correctness.

Many legacy implementations across the modding and reverse engineering scene rely heavily on assumptions, fragmented documentation, or behavior copied forward without validation.

This repository aims to instead:

- Validate structures directly against binary data
- Document edge cases
- Reconstruct actual engine behavior
- Preserve technical findings in a reproducible way

Where possible, implementations are derived from observed behavior rather than speculation.

---

## Community Collaboration

This repository is open to everyone.

Whether your interests are:

- Reverse engineering
- Game preservation
- Engine internals
- Asset systems
- Binary formats
- Tooling development
- Research documentation

—you are welcome here.

Contributions, corrections, experiments, technical discussions, and new findings are encouraged.

---

# Vision

The long-term vision is to create a reliable and well-documented ecosystem for researching and interacting with classic Call of Duty engine technology.

Not merely a parser.

Not merely a tool.

But a clean, extensible, research-grade foundation capable of preserving technical knowledge that would otherwise disappear over time.

---

# Status

> Active early-stage development

The project is evolving rapidly as additional research is validated and new systems are implemented.

Expect:
- Frequent structural changes
- Incomplete systems
- Experimental implementations
- Ongoing refactors

---

# Contributing

Contributions are welcome.

If you discover inaccuracies, undocumented behaviors, edge cases, or improvements, feel free to open:

- Issues
- Pull requests
- Research discussions

The goal is collaborative accuracy and long-term maintainability.

---

# Disclaimer

This repository is intended for educational, research, preservation, and interoperability purposes only.

All respective game assets, trademarks, and intellectual property belong to their original owners.
