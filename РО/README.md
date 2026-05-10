# РО

Актуальные исходники PDF находятся в `src`.

- `src/sections/*.typ` — основной текст разделов.
- `src/body.typ` — порядок подключения разделов.
- `src/main.typ` — входной файл сборки.
- `../term-paper.yaml` — общие данные титульника.
- `../shared/typst/core.typ` — общий шаблон оформления.

Собрать PDF:

```powershell
.\build.ps1
```
