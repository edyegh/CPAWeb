# Git hooks

Այս պանակի hook-երը կիսվում են թիմի հետ (`.git/hooks`-ը չի commit-վում):

## Մեկանգամյա setup (յուրաքանչյուր developer, clone-ից հետո)

```sh
git config core.hooksPath .githooks
```

## Hooks

### `post-checkout`

Branch փոխելիս ավտոմատ պատճենում է համապատասխան env ֆայլը `.env`-ի մեջ:

| Branch    | Source         |
|-----------|----------------|
| `master`  | `.env.master`  |
| `preprod` | `.env.preprod` |

Այլ branch-երի դեպքում `.env`-ը մնում է անփոփոխ:

`.env*` ֆայլերը `.gitignore`-ում են (գաղտնաբառեր), այնպես որ
`.env.master` և `.env.preprod`-ը պետք է ձեռքով ստեղծել յուրաքանչյուր մեքենայի վրա:

> **Ուշադրություն:** hook-երը պետք է լինեն LF line ending-ով:
> CRLF-ի դեպքում shebang-ը դառնում է `#!/bin/sh\r` և Windows-ի վրա չի աշխատում:
> Դա արդեն ապահովված է `.gitattributes`-ի `.githooks/** text eol=lf` կանոնով:
