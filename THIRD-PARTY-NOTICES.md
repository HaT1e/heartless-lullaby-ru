# Сторонние компоненты и права

## Игра

**Vampire: The Masquerade — Heartless Lullaby** и все её материалы
© Entalto Studios. **Vampire: The Masquerade** © Paradox Interactive AB.
Все права принадлежат правообладателям.

Этот репозиторий — неофициальная некоммерческая работа фанатов. Она не одобрена
и не аффилирована с Entalto Studios или Paradox Interactive. Игра в репозитории
не распространяется; для сборки и игры нужна легально приобретённая копия.

## BepInEx

Готовые релизы содержат **BepInEx 5.4.23.3 x64** — загрузчик модов для Unity.
BepInEx распространяется по лицензии **LGPL-2.1**:

- https://github.com/BepInEx/BepInEx
- https://github.com/BepInEx/BepInEx/blob/master/LICENSE

Исходный код BepInEx доступен по ссылке выше. При необходимости загрузчик можно
заменить собственной сборкой по условиям LGPL-2.1.

Вместе с BepInEx поставляются его зависимости (HarmonyX, MonoMod, Mono.Cecil и др.)
на условиях их собственных лицензий; сведения о них есть в дистрибутиве BepInEx.

## Roslyn

`tools/fetch_deps.py` скачивает **Microsoft.Net.Compilers.Toolset 4.11.0**
(компилятор Roslyn) с NuGet для локальной сборки. Roslyn распространяется
по лицензии MIT:

- https://github.com/dotnet/roslyn
- https://licenses.nuget.org/MIT

Roslyn не входит в релизный архив и не хранится в репозитории.

## Шрифты

В патч включены пять шрифтов под лицензией **SIL Open Font License 1.1**.
Полные тексты лицензий лежат в `assets/fonts_licenses/`.

| Шрифт | Автор |
|---|---|
| Oswald SemiBold | The Oswald Project Authors |
| PT Mono | ParaType |
| Old Standard TT | Alexey Kryukov |
| Rubik Wet Paint | The Rubik Project Authors |
| Russo One | Jovanny Lemonad |

## Перерисованные изображения

Файлы в `assets/sprites_ru/` — перерисованные надписи поверх игровых изображений.
Они являются производными от материалов игры и включены только в бесплатный
фанатский патч. Права на исходные изображения принадлежат Entalto Studios.
Использовать их отдельно от проекта в коммерческих целях нельзя.

## Текст перевода

Русский текст перевода распространяется по лицензии **CC BY-NC-SA 4.0**.
Подробности и полный текст — в [LICENSE.txt](LICENSE.txt).
