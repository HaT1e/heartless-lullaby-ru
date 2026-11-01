# Русская локализация Vampire: The Masquerade — Heartless Lullaby

Неофициальный фанатский перевод **Vampire: The Masquerade — Heartless Lullaby** на русский язык.
Перевод бесплатный, не одобрен правообладателями и не содержит файлов игры.

- [Скачать последнюю версию](../../releases/latest)
- [Руководство по установке для Steam](docs/STEAM_GUIDE_RU.txt)
- [Описание релиза для игроков](docs/README_RU.txt)

## Что это

Это BepInEx-плагин, который подставляет русский текст и заранее перерисованные надписи
во время работы игры. Он не переписывает `VTM Heartless Lullaby.exe`, не меняет оригинальные
ассеты на диске и не требует переустановки игры.

Что внутри:

- 1086 переведённых реплик в шести главах;
- интерфейс, меню, настройки, загрузочные экраны, журнал заданий, лист персонажа;
- имена актёров, предметов и подписи говорящих, включая цветные префиксы реплик;
- 96 перерисованных изображений с вшитым текстом;
- пять кириллических шрифтов, подобранных под оригинальные;
- даты и время в русском формате.

Плагин собирается из исходников в этом репозитории; готовый архив с BepInEx лежит
в разделе **Releases**. В репозитории нет исполняемых файлов игры, английского скрипта
и бинарников BepInEx — они не нужны для хранения исходников и не распространяются здесь.

## Установка

1. Закройте игру.
2. Скачайте архив из **Releases**.
3. Распакуйте его в папку игры, где лежит `VTM Heartless Lullaby.exe`, так, чтобы
   `winhttp.dll`, `doorstop_config.ini`, `.doorstop_version` и папка `BepInEx`
   оказались рядом с экзешником.
4. Запускайте игру через Steam (или обычным способом в GOG).

Подробная инструкция, проверка и решение проблем — в [руководстве для Steam](docs/STEAM_GUIDE_RU.txt)
и [README_RU.txt](docs/README_RU.txt).

## Сборка из исходников

### Требования

- Windows x64;
- установленная игра (Steam или GOG);
- Python 3.10+ (в примерах используется лаунчер `py -3`; если команда `python`
  уже указывает на Python 3.10+, можно использовать её);
- BepInEx 5.4.23.3 x64 — можно установить в игру или скачать скриптом;
- .NET Framework 4.x (для запуска Roslyn `csc.exe`).

### Быстрый старт

```powershell
cd "C:\путь\к\HeartlessLullabyRU"

# Скачать Roslyn и BepInEx в tools/deps (один раз)
py -3 tools\fetch_deps.py

# Собрать плагин; путь к игре можно не указывать, если она лежит в стандартной папке Steam
py -3 tools\build_plugin.py

# Собрать папку релиза и zip в dist/
py -3 tools\package_release.py
```

Если игра установлена в нестандартное место:

```powershell
$env:HL_GAME_DIR = "D:\Games\Vampire The Masquerade - Heartless Lullaby"
py -3 tools\build_plugin.py
```

Или явно:

```powershell
py -3 tools\build_plugin.py --game "D:\Games\Vampire The Masquerade - Heartless Lullaby"
```

Скрипт сборки использует управляемые сборки из папки игры
`VTM Heartless Lullaby_Data\Managed`, поэтому без установленной игры собрать плагин нельзя.
Готовый DLL появляется в `build/out/HeartlessLullabyRU.dll`.

### Параметры сборки

`tools/build_plugin.py`:

- `--game` — папка игры (или переменная `HL_GAME_DIR`);
- `--csc` — путь к Roslyn `csc.exe` (или `HL_CSC` / `CSC_PATH`);
- `--bepinex-core` — папка с `BepInEx.dll` и `0Harmony.dll` (или `HL_BEPINEX_CORE`).

`tools/package_release.py`:

- `--out` — куда собрать релиз (по умолчанию `dist/`);
- `--bepinex` — распакованный BepInEx 5.4.23.3 x64 (по умолчанию `tools/deps/bepinex`);
- `--no-zip` — собрать только папку, без архива.

### Проверка перевода

```powershell
py -3 tools\validate_translation.py
```

Скрипт проверяет количество строк, имена, интерфейс, картинки, шрифты, JSON, парность
BBCode-тегов и смешанные кириллическо-латинские слова. Он не требует установленной игры
и запускается в GitHub Actions на каждый push.

## Структура репозитория

```text
assets/                 данные перевода, шрифты и перерисованные надписи
  fonts/                пять TTF-шрифтов (OFL)
  fonts_licenses/       тексты лицензий шрифтов
  sprites_ru/           96 изображений с русскими надписями
  ru_dialogue.json      диалоги: ключ "глава:реплика"
  ru_ui.json            интерфейс, имена, задания и служебные строки
  ru_sprites.json       карта замены изображений
docs/                   README для архива, глоссарий, Steam-руководство и релиз-ноты
src/HeartlessLullabyRU/ исходники плагина на C#
tools/                  сборка, упаковка, проверка и загрузка зависимостей
.github/workflows/      CI-проверка перевода
```

## Формат данных

`assets/ru_dialogue.json`:

```json
{
  "lines": {
    "26:1": { "text": "Наш герой — Зак Райли…" }
  }
}
```

`assets/ru_ui.json` содержит секции `uis`, `tmp`, `questJournal`, `actors`, `items`, `time`.
`assets/ru_sprites.json` сопоставляет имя оригинального спрайта с файлом из `sprites_ru/`.

Если меняете перевод:

1. Отредактируйте JSON.
2. Запустите `py -3 tools/validate_translation.py`.
3. Пересоберите плагин и релиз.

Версия указывается в двух местах: `src/HeartlessLullabyRU/Plugin.cs` и
`docs/README_RU.txt`. Валидатор проверяет, что они совпадают.

## Релизы

1. Поднимите версию в `Plugin.cs` и `docs/README_RU.txt`.
2. Добавьте запись в `CHANGELOG.md`.
3. Соберите плагин и архив.
4. Создайте GitHub Release с тегом `vX.Y.Z` и приложите `dist/HeartlessLullabyRU_vX.Y.Z.zip`.

Пошаговая инструкция — в [docs/PUBLISH_GITHUB.md](docs/PUBLISH_GITHUB.md).
Готовое описание релиза v1.1.5 — в [docs/RELEASE_NOTES_v1.1.5.md](docs/RELEASE_NOTES_v1.1.5.md).

## Лицензии и права

- Код плагина — MIT, см. [LICENSE-CODE.txt](LICENSE-CODE.txt).
- Русский текст перевода и перерисованные надписи — CC BY-NC-SA 4.0,
  полный текст в [LICENSE.txt](LICENSE.txt).
- Шрифты — SIL Open Font License 1.1, тексты лежат в `assets/fonts_licenses/`.
- BepInEx — LGPL-2.1, в репозитории не хранится.
- Игра и её ассеты © Entalto Studios; Vampire: The Masquerade © Paradox Interactive AB.
  Перерисованные изображения являются производными от игровых материалов и входят в бесплатный
  фанатский патч; права на исходные изображения принадлежат Entalto Studios.

Этот проект не одобрен и не аффилирован с Entalto Studios или Paradox Interactive.
Перевод нельзя продавать или включать в платные сборки.

## Благодарности

- Entalto Studios — за игру.
- Paradox Interactive — за вселенную Vampire: The Masquerade.
- BepInEx, HarmonyX и Roslyn — за инструменты.
- Авторы шрифтов Oswald, PT Mono, Old Standard TT, Rubik Wet Paint и Russo One.
