# Contributing to NearU Backend

Thank you for your interest in contributing to NearU Backend! We welcome contributions from everyone.

## 📋 Table of Contents

- [Code of Conduct](#code-of-conduct)
- [How Can I Contribute?](#how-can-i-contribute)
- [Development Setup](#development-setup)
- [Coding Standards](#coding-standards)
- [Commit Guidelines](#commit-guidelines)
- [Pull Request Process](#pull-request-process)
- [Issue Reporting](#issue-reporting)

## 🤝 Code of Conduct

This project adheres to a Code of Conduct. By participating, you are expected to uphold this code. Please be respectful and considerate in all interactions.

## 💡 How Can I Contribute?

### Reporting Bugs

Before creating bug reports, please check existing issues. When creating a bug report, include:

- **Clear title and description**
- **Steps to reproduce** the issue
- **Expected behavior** vs actual behavior
- **Screenshots** (if applicable)
- **Environment details** (.NET version, OS, etc.)

### Suggesting Enhancements

Enhancement suggestions are tracked as GitHub issues. When creating an enhancement suggestion, include:

- **Clear title and description**
- **Use case** - why is this enhancement needed?
- **Proposed solution** - how should it work?
- **Alternative solutions** you've considered

### Pull Requests

1. **Fork the repository** and create your branch from `main`
2. **Follow the coding standards** outlined below
3. **Write clear commit messages** following our commit guidelines
4. **Include tests** for new features or bug fixes
5. **Update documentation** if needed
6. **Ensure all tests pass** before submitting
7. **Submit a pull request** with a clear description

## 🛠️ Development Setup

1. **Clone your fork**
   ```bash
   git clone https://github.com/YOUR-USERNAME/NearU-Backend.git
   cd NearU-Backend
   ```

2. **Add upstream remote**
   ```bash
   git remote add upstream https://github.com/ORIGINAL-OWNER/NearU-Backend.git
   ```

3. **Install dependencies**
   ```bash
   dotnet restore
   ```

4. **Create a branch**
   ```bash
   git checkout -b feature/your-feature-name
   ```

5. **Make your changes** and test thoroughly

6. **Run tests**
   ```bash
   dotnet test
   ```

## 📝 Coding Standards

### C# Style Guide

- Follow the `.editorconfig` settings in the repository
- Use **PascalCase** for class names, method names, and public members
- Use **camelCase** for local variables and parameters
- Use **_camelCase** for private fields
- Interfaces should start with `I` (e.g., `IUserService`)

### Code Quality

- **Keep methods small** - each method should do one thing
- **Use meaningful names** - variables and methods should be self-documenting
- **Add XML comments** for public APIs
  ```csharp
  /// <summary>
  /// Gets a user by their unique identifier.
  /// </summary>
  /// <param name="userId">The user's ID.</param>
  /// <returns>The user object if found; otherwise, null.</returns>
  public async Task<User?> GetUserByIdAsync(int userId)
  ```
- **Avoid magic numbers** - use named constants
- **Handle exceptions appropriately** - don't swallow exceptions
- **Use async/await** for I/O operations

### Testing

- Write **unit tests** for all business logic
- Follow **Arrange-Act-Assert** pattern
- Use meaningful test names: `MethodName_Scenario_ExpectedResult`
  ```csharp
  [Fact]
  public async Task GetUserById_UserExists_ReturnsUser()
  {
      // Arrange
      var userId = 1;
      
      // Act
      var result = await _userService.GetUserByIdAsync(userId);
      
      // Assert
      Assert.NotNull(result);
      Assert.Equal(userId, result.Id);
  }
  ```

## 📦 Commit Guidelines

We follow [Conventional Commits](https://www.conventionalcommits.org/) for clear commit history:

### Commit Message Format

```
<type>(<scope>): <subject>

<body>

<footer>
```

### Types

- `feat:` - New feature
- `fix:` - Bug fix
- `docs:` - Documentation changes
- `style:` - Code style changes (formatting, missing semi-colons, etc.)
- `refactor:` - Code refactoring (no functionality change)
- `perf:` - Performance improvements
- `test:` - Adding or updating tests
- `chore:` - Maintenance tasks, dependency updates

### Examples

```bash
feat(auth): add JWT authentication

Implemented JWT token generation and validation
for user authentication endpoints.

Closes #123
```

```bash
fix(api): resolve null reference in user controller

Added null check before accessing user properties
to prevent NullReferenceException.

Fixes #456
```

## 🔄 Pull Request Process

1. **Update your branch** with the latest changes from upstream
   ```bash
   git fetch upstream
   git rebase upstream/main
   ```

2. **Push your changes** to your fork
   ```bash
   git push origin feature/your-feature-name
   ```

3. **Create a pull request** with:
   - Clear title describing the change
   - Description of what changed and why
   - Link to related issues
   - Screenshots (if UI changes)

4. **Code review**
   - Address reviewer feedback
   - Make requested changes
   - Keep discussions professional and constructive

5. **Merge**
   - Once approved, a maintainer will merge your PR
   - Delete your branch after merge

## 🐛 Issue Reporting

### Before Submitting

- Check existing issues to avoid duplicates
- Ensure you're using the latest version
- Try to reproduce the issue in a clean environment

### Creating an Issue

Use our issue templates and include:

**For Bugs:**
- Clear, descriptive title
- Steps to reproduce
- Expected vs actual behavior
- Environment details
- Error logs/screenshots

**For Features:**
- Clear description of the feature
- Use case and benefits
- Proposed implementation (optional)

## 🙏 Thank You!

Your contributions make this project better for everyone. We appreciate your time and effort!

## 📞 Questions?

If you have questions, feel free to:
- Open a discussion on GitHub
- Ask in pull request comments
- Contact the maintainers

---

Happy coding! 🚀
