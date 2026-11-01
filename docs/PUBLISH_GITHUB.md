# Публикация репозитория на GitHub

Репозиторий уже подготовлен в папке:

```text
C:\Users\Серёжа\Desktop\Готовые переводы\Heartless Lullaby RU\github\HeartlessLullabyRU
```

Опубликованный репозиторий:

- https://github.com/HaT1e/heartless-lullaby-ru
- Релизы: https://github.com/HaT1e/heartless-lullaby-ru/releases/latest

Внутри лежат исходники плагина, перевод, шрифты, перерисованные изображения,
инструменты сборки, лицензии и CI-проверка. Игровых файлов и бинарников BepInEx
в репозитории нет.

## 1. Проверка перед публикацией

Откройте PowerShell и перейдите в папку репозитория:

```powershell
cd "C:\Users\Серёжа\Desktop\Готовые переводы\Heartless Lullaby RU\github\HeartlessLullabyRU"
```

Проверьте, что в папке нет лишнего:

```powershell
Get-ChildItem
```

Ожидаемые элементы:

```text
.github  assets  docs  src  tools
.gitattributes  .gitignore  CHANGELOG.md
LICENSE.txt  LICENSE-CODE.txt  README.md  THIRD-PARTY-NOTICES.md
```

Не должны попасть в репозиторий:

- `build/` — результаты локальной сборки;
- `tools/deps/` — скачанные Roslyn и BepInEx;
- `dist/` — готовые архивы (они лежат вне папки репозитория);
- игровые `.exe`, `.dll`, архивы Unity и `source_database.json`;
- личные скриншоты, логи и файлы с паролями.

Эти пути уже исключены в `.gitignore`. Проверить исключения можно так:

```powershell
git check-ignore -v build tools\deps
```

Если Git ещё не инициализирован, команда создаст ошибку — это нормально,
инициализация будет на следующем шаге.

## 2. Создайте пустой репозиторий на GitHub

1. Войдите в GitHub и нажмите **New repository**.
2. Имя: `heartless-lullaby-ru`.
3. Описание:

   ```text
   Неофициальный русский перевод Vampire: The Masquerade — Heartless Lullaby на BepInEx: диалоги, интерфейс, шрифты и 96 перерисованных изображений.
   ```

4. Выберите **Public**.
5. Не включайте **Add a README file**, **Add .gitignore** и **Choose a license**:
   эти файлы уже есть в локальной папке.
6. Нажмите **Create repository**.

Если репозиторий уже создан, этот раздел можно пропустить и перейти к push.

GitHub покажет команды для пустого репозитория. Используйте вариант ниже.

## 3. Первый коммит и отправка

В PowerShell:

```powershell
cd "C:\Users\Серёжа\Desktop\Готовые переводы\Heartless Lullaby RU\github\HeartlessLullabyRU"

git init
git branch -M main
git add .
git status
```

В `git status` проверьте список файлов. Должны быть исходники, JSON, шрифты,
PNG, документы и workflow. Не должно быть `build/`, `tools/deps/`, EXE и DLL игры.

Если всё верно:

```powershell
git commit -m "Initial commit: Russian localization for Heartless Lullaby v1.1.5"
git remote add origin https://github.com/HaT1e/heartless-lullaby-ru.git
git push -u origin main
```

Адрес репозитория уже указан для аккаунта `HaT1e`. Если вы используете
другой аккаунт, замените его. При первом push откроется окно входа
GitHub или Git Credential Manager. Пароль от аккаунта GitHub не подойдёт:
используйте вход через браузер или personal access token.

Если репозиторий уже был создан ранее и `git remote add` выдаёт ошибку
«remote origin already exists», проверьте адрес:

```powershell
git remote -v
```

и при необходимости замените его:

```powershell
git remote set-url origin https://github.com/HaT1e/heartless-lullaby-ru.git
```

## 4. Проверьте GitHub Actions

После push откройте вкладку **Actions** в репозитории. Workflow
**Validate translation** должен завершиться зелёной галочкой.

Он проверяет:

- синтаксис Python-скриптов;
- 1086 строк диалогов;
- 96 уникальных строк интерфейса;
- 24 имени актёров и 8 предметов;
- 96 замен изображений;
- пять шрифтов и лицензии;
- парность BBCode-тегов;
- смешанные кириллическо-латинские слова;
- совпадение версии в `Plugin.cs` и `docs/README_RU.txt`.

Если workflow красный, откройте лог шага и исправьте ошибку. Локально ту же
проверку можно запустить командой:

```powershell
py -3 tools\validate_translation.py
```

В примерах используется лаунчер `py -3`. Если команда `python` уже указывает
на Python 3.10+, можно писать `python tools\validate_translation.py`.

## 5. Заполните описание репозитория

На странице репозитория нажмите шестерёнку рядом с **About** и добавьте:

- **Description:** `Неофициальный русский перевод Vampire: The Masquerade — Heartless Lullaby на BepInEx`;
- **Website:** ссылка на Steam-руководство после его публикации;
- **Topics:** `vampire-the-masquerade`, `heartless-lullaby`, `russian`,
  `localization`, `translation`, `bepinex`, `unity`, `fan-translation`.

Не ставьте ссылку на Steam-руководство, пока оно не опубликовано.

## 6. Создайте релиз с архивом

Архив уже собран:

```text
C:\Users\Серёжа\Desktop\Готовые переводы\Heartless Lullaby RU\dist\HeartlessLullabyRU_v1.1.5.zip
```

Его размер около 23 МБ, внутри 29 файлов: BepInEx, плагин, README, лицензия
перевода и лицензии шрифтов. Не загружайте в релиз всю папку `dist` или исходники игры.

1. Откройте репозиторий на GitHub.
2. Справа выберите **Releases** → **Draft a new release**.
3. Нажмите **Choose a tag** и введите `v1.1.5`, затем **Create new tag on publish**.
4. **Release title:** `v1.1.5 — русская локализация`.
5. Вставьте описание из файла
   [RELEASE_NOTES_v1.1.5.md](RELEASE_NOTES_v1.1.5.md).

6. Прикрепите файл `HeartlessLullabyRU_v1.1.5.zip` в блоке **Attach binaries**.
7. Убедитесь, что стоит галочка **Set as the latest release**.
8. Нажмите **Publish release**.

После публикации проверьте, что ссылка
`https://github.com/HaT1e/heartless-lullaby-ru/releases/latest` открывается
и архив скачивается. SHA-256 скачанного файла должен совпасть с указанным.

Если вы пересобирали архив и хеш изменился, обновите его в описании релиза
и в Steam-руководстве.

## 7. Тег в Git

Если вы создали тег через GitHub Release, отдельный `git tag` не обязателен.
Если хотите продублировать тег в репозитории:

```powershell
git tag -a v1.1.5 -m "Русская локализация 1.1.5"
git push origin v1.1.5
```

Не создавайте тег `v1.1.5` дважды: если он уже есть на GitHub, команда
`git push origin v1.1.5` завершится ошибкой «already exists». Это нормально.

## 8. Свяжите релиз, Steam и README

1. Опубликуйте Steam-руководство по инструкции
   [STEAM_GUIDE_PUBLISH.md](STEAM_GUIDE_PUBLISH.md).
2. Скопируйте ссылку на руководство.
3. Добавьте её:
   - в поле **Website** репозитория;
   - в README, в блок ссылок в начале;
   - в описание релиза.
4. Ссылки в Steam-руководстве уже подставлены под репозиторий
   `HaT1e/heartless-lullaby-ru`; при переименовании репозитория обновите их.

## 9. Следующие версии

Порядок для каждого нового релиза:

1. Измените версию в `src/HeartlessLullabyRU/Plugin.cs` и
   `docs/README_RU.txt` — она должна совпадать.
2. Добавьте запись в `CHANGELOG.md`.
3. Проверьте перевод:

   ```powershell
   py -3 tools\validate_translation.py
   ```

4. При необходимости пересоберите плагин и архив:

   ```powershell
   py -3 tools\build_plugin.py --game "C:\путь\к\игре"
   py -3 tools\package_release.py
   ```

5. Закоммитьте изменения:

   ```powershell
   git add .
   git status
   git commit -m "Release vX.Y.Z"
   git push
   ```

6. Создайте тег и новый GitHub Release, приложите новый zip.
7. Обновите версию, изменения и SHA-256 в Steam-руководстве.

Не перезаписывайте старые релизы: новый архив должен быть в новом релизе,
чтобы у игроков оставалась возможность откатиться.

## Частые проблемы

- **Git не установлен.** Установите Git for Windows или используйте GitHub Desktop.
- **`git push` просит пароль.** Используйте Git Credential Manager, вход через
  браузер или personal access token; обычный пароль GitHub не принимается.
- **Слишком большой репозиторий.** Проверьте, что не добавлены `build/`,
  `tools/deps/`, игровые DLL и архивы: `git ls-files | Select-String "deps|build|\.zip$"`.
- **Actions не запускается.** Убедитесь, что файл
  `.github/workflows/validate.yml` попал в коммит и в репозитории включены Actions.
- **Русские имена файлов отображаются некорректно.** В Git для Windows включите
  `git config core.quotepath false`; на содержимое репозитория это не влияет.
- **Случайно добавили лишний файл.** Удалите его из индекса командой
  `git rm --cached "путь"`, добавьте путь в `.gitignore` и закоммитьте.
