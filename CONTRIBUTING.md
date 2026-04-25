# Contributing to NearU Frontend

First off, thank you for considering contributing to NearU! It's people like you that make NearU such a great tool for discovering nearby services and campus-life essentials.

Following these guidelines helps to communicate that you respect the time of the developers managing and developing this open source project. In return, they should reciprocate that respect in addressing your issue, assessing changes, and helping you finalize your pull requests.

## Code of Conduct

By participating in this project, you are expected to uphold our Code of Conduct. Please be welcoming, inclusive, and respectful to all members of the community.

## Getting Started

If you haven't already, please review the [README.md](./README.md) for instructions on how to set up the project locally. 

Key technologies you'll be working with:
- React 18
- TypeScript
- Vite 6
- React Router 7
- Tailwind CSS 4
- Radix UI & Material UI components

## How to Contribute

### 1. Find an Issue
Look for open issues in the issue tracker. If you want to work on something specific that isn't listed, please open an issue first to discuss it before you start writing code.

### 2. Branch Naming Convention
We follow a specific branching convention to keep our repository organized. Create a branch from `main`:

```bash
git checkout main
git pull origin main
git checkout -b <type>/<short-description>
```

Common types:
- `feat/`: New feature
- `fix/`: Bug fix
- `docs/`: Documentation changes
- `refactor/`: Code refactoring without adding features or fixing bugs
- `style/`: Formatting, missing semi-colons, etc.
- `test/`: Adding missing tests

Example: `feat/transport-filter-chips` or `fix/login-validation-error`

### 3. Making Changes
- **TypeScript**: Ensure your code is properly typed. Avoid using `any` unless absolutely necessary.
- **Component Structure**: Keep components small and focused. Reusable UI primitives belong in `src/app/components/ui/`, while domain-specific components belong in `src/app/components/`.
- **Styling**: We use Tailwind CSS for styling. Prefer utility classes over custom CSS where possible.
- **Type Checking**: Before committing, run `npm run typecheck` to ensure there are no TypeScript compilation errors.

### 4. Commit Message Convention
Commit clearly and frequently. We use conventional commits.

```bash
git commit -m "type(scope?): subject"
```

Examples:
- `feat: add transport filter chips`
- `fix(auth): handle expired token edge case`
- `style: format login page inputs`

### 5. Pull Request Process
1. Push your branch to the remote repository:
   ```bash
   git push -u origin <branch-name>
   ```
2. Open a Pull Request against the `main` branch.
3. Ensure your PR description clearly describes the problem and solution. Include the relevant issue number if applicable (e.g., "Fixes #123").
4. Request review from maintainers.
5. Address any feedback provided during the review process. 
6. Once approved and all status checks pass, your PR will be merged.

## Reporting Bugs

Bugs are tracked as GitHub issues. When creating an issue, please provide the following information:
- A clear and descriptive title.
- Steps to reproduce the behavior.
- Expected behavior vs. actual behavior.
- Environment details (Browser, OS, Node version).
- Any relevant logs, screenshots, or error messages.

## Suggesting Enhancements

Enhancement suggestions are also tracked as GitHub issues. When creating an issue, please provide:
- A clear and descriptive title.
- A detailed description of the proposed feature.
- Explain *why* this enhancement would be useful to most NearU users.
- Some examples of how the feature would work, if applicable.

Thank you for contributing!
