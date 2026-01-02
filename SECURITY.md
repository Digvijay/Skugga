# Security Policy

## Supported Versions

We release patches for security vulnerabilities for the following versions:

| Version | Supported          |
| ------- | ------------------ |
| 1.1.x   | :white_check_mark: |
| 1.0.x   | :white_check_mark: |
| < 1.0   | :x:                |

## Reporting a Vulnerability

The Skugga team takes security bugs seriously. We appreciate your efforts to responsibly disclose your findings and will make every effort to acknowledge your contributions.

### How to Report

**Please DO NOT report security vulnerabilities through public GitHub issues.**

Instead, please report them via email to:
- **Email**: security@[your-domain].com (or create a GitHub Security Advisory)

To report a vulnerability:

1. **Preferred Method**: Use [GitHub Security Advisories](https://github.com/Digvijay/Skugga/security/advisories/new)
   - Navigate to the Security tab
   - Click "Report a vulnerability"
   - Fill in the details

2. **Alternative**: Email the security team with:
   - Type of issue (e.g., buffer overflow, SQL injection, cross-site scripting, etc.)
   - Full paths of source file(s) related to the manifestation of the issue
   - The location of the affected source code (tag/branch/commit or direct URL)
   - Any special configuration required to reproduce the issue
   - Step-by-step instructions to reproduce the issue
   - Proof-of-concept or exploit code (if possible)
   - Impact of the issue, including how an attacker might exploit it

### What to Expect

- You should receive an acknowledgment within 48 hours
- We will send a more detailed response within 7 days indicating the next steps in handling your report
- After the initial reply to your report, we will keep you informed of the progress towards a fix and full announcement
- We may ask for additional information or guidance

## Security Update Process

1. **Receive Report**: Security team receives and acknowledges the vulnerability report
2. **Assessment**: Team assesses the severity and impact
3. **Fix Development**: Develop and test a fix
4. **Coordinated Disclosure**: Coordinate release timing with the reporter
5. **Release**: Publish fix and security advisory
6. **Credit**: Credit reporter in security advisory (if desired)

## Security Best Practices for Users

When using Skugga in your projects:

### 1. Keep Dependencies Updated

```bash
# Regularly check for updates
dotnet list package --outdated

# Update to latest
dotnet add package Skugga
```

### 2. Use Supported Versions

Always use the latest supported version of Skugga. Security patches are only provided for supported versions.

### 3. AOT Compilation

Skugga is designed for Native AOT compilation, which provides several security benefits:
- No runtime code generation
- Smaller attack surface
- Faster cold starts (reducing timing attack windows)

### 4. Code Generation Security

Skugga uses source generators at compile-time:
- No reflection or dynamic code generation at runtime
- All mock code is generated during build
- Code can be audited before deployment

## Known Security Considerations

### Compile-Time Only

Skugga is a compile-time library with zero runtime reflection:
- ✅ **Safe**: No runtime code generation
- ✅ **Safe**: No use of `System.Reflection` in production code
- ✅ **Safe**: All mocking logic happens at compile-time via source generators

### Test Code Only

Skugga is designed for **test code only**:
- Should not be used in production application code
- Mock objects should only exist in test projects
- Generator output is only used during testing

### Dependencies

Skugga has minimal dependencies:
- `Microsoft.CodeAnalysis.CSharp` (generator only, not in deployed code)
- `Microsoft.SourceLink.GitHub` (development/debugging only)

All dependencies are actively maintained and monitored for security vulnerabilities.

## Security Scanning

This project uses:
- **Dependabot**: Automated dependency updates and security alerts
- **CodeQL**: Static analysis for security issues
- **NuGet Vulnerability Scanning**: Automated checks for vulnerable packages

## Disclosure Policy

When we receive a security bug report, we will:

1. Confirm the problem and determine affected versions
2. Audit code to find any similar problems
3. Prepare fixes for all supported versions
4. Release new versions as soon as possible

## Comments on This Policy

If you have suggestions on how this process could be improved, please submit a pull request or open an issue.

## Acknowledgments

We would like to thank the following individuals for responsibly disclosing security vulnerabilities:

- *None yet - you could be the first!*

---

**Last Updated:** January 1, 2026  
**Version:** 1.0
