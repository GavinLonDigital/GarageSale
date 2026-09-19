# GarageSale

## Project overview

GarageSale is an e-commerce web application built with Blazor Server.

The application is developed and run using Docker.

## General development guidance

Follow the existing project structure, architectural patterns, naming
conventions, and coding style when making changes.

Keep changes focused on the task requested by the user.

Avoid unrelated refactoring or restructuring unless it is necessary to
complete the requested task.

Before introducing a new dependency, library, framework, or development
tool, determine whether the existing project already provides the required
functionality.

## Existing behavior

Unless the task explicitly requires behavioral changes, preserve existing:

- application behavior
- routes
- navigation
- data bindings
- authentication behavior
- component interactions
- business logic

Do not assume that a presentation change requires a functional or
architectural change.

## Development environment

The Docker-based development environment is already configured.

Do not modify Docker, Docker Compose, development tooling, project
configuration, or environment configuration unless the requested task
requires those changes.

## Scope

Prefer focused changes to the smallest reasonable set of files.

Do not modify unrelated files simply because an opportunity for cleanup
or refactoring is discovered.

When requirements are ambiguous, prefer preserving existing application
behavior over introducing new behavior.