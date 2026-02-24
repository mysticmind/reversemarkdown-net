CommonMark test data

Download commonmark.json from the CommonMark spec repository and save it here:

https://raw.githubusercontent.com/commonmark/commonmark-spec/master/commonmark.json

Local data-driven test cases

Add cases to cases.json with the following fields:

- id: unique identifier, used for test naming
- html: source HTML input
- expected: expected markdown output (inline)
- expectedFile: filename of a snapshot in src/ReverseMarkdown.Test (optional alternative to expected)
- config: optional Config values (CommonMark, SlackFlavored, etc.)
- tags: optional tags for filtering via RM_TEST_TAGS

Example:

{
  "id": "basic-strong",
  "html": "This is a <strong>sample</strong> paragraph",
  "expected": "This is a **sample** paragraph"
}
