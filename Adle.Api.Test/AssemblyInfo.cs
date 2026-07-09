using Microsoft.VisualStudio.TestTools.UnitTesting;

// These are HTTP integration tests over a single shared host + the static IoC
// container + the real database; run them serially to avoid cross-test races.
[assembly: DoNotParallelize]
