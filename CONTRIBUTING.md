# Contributing

Thanks for helping to improve Mafesoft.TinyCsv! Bug reports, ideas and pull requests are welcome.

- **Bugs and proposals:** open an [issue](https://github.com/fmazzant/TinyCsv/issues) first, with a minimal
  example (the model, the options and a few rows of the file). For an API change, agree on the design in the issue
  before writing the code.
- **Security problems:** do not open a public issue, see [SECURITY.md](SECURITY.md).

## Build and test

Requirements: the .NET 10 SDK and the .NET 8 runtime (the tests also run on .NET Framework 4.8 on Windows).

```
dotnet build -c Release TinyCsv.slnx
dotnet test -c Release TinyCsv.slnx
dotnet pack -c Release -o ./artifacts TinyCsv.slnx
```

`dotnet pack` also runs the package validation (`src/Directory.Build.props`): the public API must be the same on every
target framework. An intentional difference is recorded in `CompatibilitySuppressions.xml` (next to the `.csproj`),
regenerated with:

```
dotnet pack -c Release TinyCsv.slnx -p:ApiCompatGenerateSuppressionFile=true
```

The samples and the benchmarks reference the libraries as projects, so they must keep building after every change.
The benchmarks are described in [benchmark/README.md](benchmark/README.md).

## Code

- Nullable reference types are enabled: no new warnings, and no `!` operator without a comment that explains why it is safe.
- A record is a logical row: a quoted field may span multiple lines, and it counts as one row everywhere
  (indexes, `SkipRow`, `RowsToSkip`, error positions).
- Reading must not create a string for each record when it is not needed (`SkipRow` not set, handlers disabled):
  keep the fast path fast, and measure with the benchmarks.

## Branches and pull requests

- `main` is the default branch; work on a branch per issue (e.g. `feat/64-async-api`) and open the pull request
  against `main`, linking the issue (`Closes #64`).
- Keep the commits focused: one logical change per commit, with a message that explains why.
- Every pull request must build, pass the tests and the package validation, and update:
  - the tests;
  - the README and the `[x.y.z] - Unreleased` section of [CHANGELOG.md](CHANGELOG.md)
    ([Keep a Changelog](https://keepachangelog.com/en/1.1.0/) format).

Breaking changes are accepted only in a major version, with a note in the **Migrating to x.0** section of the README.

## Releasing (maintainers)

1. Set the same `<Version>` in `src/TinyCsv/TinyCsv.csproj` and `src/TinyCsv.AspNetCore/TinyCsv.AspNetCore.csproj`.
2. In `CHANGELOG.md` replace `Unreleased` with the release date (`## [4.0.0] - 2026-12-01`).
3. Merge into `main`, then push the tag: `git tag v4.0.0 && git push origin v4.0.0`.
4. After the first release of a major version, set `PackageValidationBaselineVersion` in `src/Directory.Build.props`
   to the released version, to catch accidental breaking changes in the minor versions.
