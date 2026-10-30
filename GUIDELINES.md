# AI Guidelines

This document outlines how you may (and how you may not) make use of AI tooling when interacting with this repository. It is split up in a list of **do's** and **don'ts**. This document may evolve over time, so in case you make use of AI tooling often, keep an eye on this document and changes made within.

## Do's

- **Do** use AI to help **you** get a better understanding of how the game works internally (reverse engineering)
- **Do** use AI to help **you** get a better understanding of how the mod works internally
- **Do** use AI to review code that **you** wrote, and to better get an understanding of what was built.
- **Do** use AI to build quick and dirty prototypes, separate from this codebase (e.g. a demo scene in a Unity project).

## Don'ts

- **Do not** use AI to write a huge chunk of changes for you. It is imperative that you fully understand each line of code that is submitted in a PR.
- **Do not** use AI to write function, property or field descriptions (`<summary>`), as **you** must understand the purpose of these fields and functions yourself, which you can use to write your own description (translating from your language is fine if you struggle with English).
- **Do not** use AI to communicate for you in PRs and issues. If you struggle with English, write your queries and responses in your own language first, and then translate them with any tool of your choice (AI is allowed there). Ideally, have both your original text and the English version, putting either one in a block quote (`> {text}`).

## Disclosure requirements

In general, you must disclose clearly when you have used AI tooling to make changes to the code, even if it's very minimal. However there are some exceptions where disclosing the use of AI tooling is not needed:

- AI was only used to help you understand how the game or the mod works
- AI was only used to review your code, without it having made changes to said code
- IDE autocomplete (includes both old-school IntelliSense and AI autocomplete)
- Translations in comments, summaries, PRs or issues (including the original text is appreciated!). However, if this mod supports localization, AI is **not** allowed to be used to translate the mod. You may however create initial drafts, but we'd like a native speaker to approve those translations.

When in doubt, disclose the use of AI tooling. This repository doesn't ban AI usage outright: if you use it responsibly, we don't have any issues with it.

## Questions?

Feel free to [create a discussion](https://github.com/DaXcess/LCVR/discussions/new?category=ai-guidelines) (do **NOT** make an issue) using the "AI Guidelines" label if you have any further questions, or if you have any changes you would like to propose to this document. Discussions (including ones about AI guidelines) must also follow these guidelines (so do not have an AI talk on your behalf, we'd like to interact with human beings).
