# Registry validator fixtures

Owner: `FND-009` (`TASK-FND-009-002`). Verification: `VER-FND-009-008`,
`VER-FND-009-009`, `VER-FND-009-011`.

Each directory is a deliberately invalid miniature specification. The validator is a
pure function from a source set to findings, so a fixture is just a handful of files
rather than a second repository.

| Fixture | Proves |
| --- | --- |
| `compliant/` | the positive control: a valid source set produces zero findings, so the four invalid fixtures below are measuring their injected defect rather than a validator that rejects everything |
| `missing/` | an identifier referenced but never defined, in prose and in a registry entry's cited requirements |
| `duplicate/` | one identifier defined twice, in a table and in a registry file |
| `dangling/` | a relative link to a file that does not exist, a link to a heading that does not exist, and a registry entry citing a nonexistent anchor |
| `malformed/` | identifier-shaped tokens that violate the grammar, plus registry shape defects: an unaccepted `tier`, an empty required field, and a section sign written as a JSON escape |

Two optional per-fixture declarations keep a fixture from depending on the real tree.
`existing-paths.txt` lists the repository paths the fixture pretends exist, for link
resolution. `discovered-tests.txt` lists the tests the fixture pretends the NUnit harness
discovered, so an `nunit` selector in `FIX-001.json` resolves against a declared list
instead of against whatever tests the repository currently has; both files are read by
`RegistrySources.ReadFixture`. `discovered-tests.txt` is deliberately nonempty in every
fixture here, because an empty inventory is itself a validator failure
(`RegistryRule.EmptyTestInventory`): a selector resolved against nothing can never be
contradicted, so a fixture with no declared tests would prove its own defect for the wrong
reason.

These files are **not part of `MechaMiner.sln`**. The repository policy keeps a
deliberately invalid fixture out of every production project, which is why they live
here beside the build-policy fixtures rather than under `tests/`.

The document file names carry the `110-`, `112-`, and `115-` prefixes on purpose: the
validator recognizes the document that owns each identifier family by filename prefix,
so a fixture can contain only the defining documents under test.
