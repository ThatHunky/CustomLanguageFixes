# Реліз на Nexus Mods — чекліст

Nexus не має API для завантаження модів (тільки читання), тож сторінки створюються вручну через сайт.
Описи готові в цій теці: `android-description.bbcode`, `pc-description.bbcode` (Nexus розуміє BBCode, не Markdown).

## Метадані сторінок

| Поле | Android | ПК |
|---|---|---|
| Name | Custom Language Fixes (Android) | Custom Language Bundle Fix |
| Category | User Interface (id 10) | User Interface (id 10) |
| Version | 2.1.0 | 1.0.1 |
| Опис | `android-description.bbcode` | `pc-description.bbcode` |
| Файл | `releases/CustomLanguageFixes-2.1.0.zip` | `releases/CustomLanguageBundleFix-1.0.1.zip` |

«(Android)» у назві сторінки лишаємо навмисно — щоб ПК-гравці не ставили мобільний мод і не писали баг-репорти.
У самому manifest назва без суфікса, бо там вона показується як заголовок меню налаштувань.

## Теги

`translation`, `localization`, `language`, `android`, `ui`, `smapi` — для Android;
`translation`, `localization`, `bundles`, `community center`, `smapi` — для ПК.

## Permissions (вкладка Permissions при створенні)

Ліцензія MIT, тож дозволяємо все, крім вимоги вказувати авторство:
- Users can upload this file to other sites: **No** (щоб не розповзались старі версії; за запитом — так)
- Users can convert this file / use assets in their own files: **Yes**
- Users can use assets without permission as long as credit is given: **Yes**
- Asset use in mods that are sold: **No** (Nexus і так забороняє платні моди)

## Donations

Nexus дозволяє посилання на зовнішні донати. Монобанка вже в описі GitHub;
на Nexus додати в полі Donations: https://send.monobank.ua/jar/9WQuPLcBwx

## Після створення сторінок — обов'язково

1. Записати modId кожної сторінки.
2. Додати в `manifest.json` ключ оновлень: `"UpdateKeys": ["Nexus:<modId>"]`
   (для Android можна залишити і GitHub-ключ: `["Nexus:<id>", "GitHub:ThatHunky/CustomLanguageFixes"]`).
3. Підняти версію (Android 2.1.1, ПК 1.0.2), перезібрати, перепакувати, залити оновлений файл.
   Без цього SMAPI не повідомлятиме користувачів про оновлення.
4. Додати посилання на сторінки Nexus у README.

## Що зробити ДО публікації

- [ ] DID тестує 2.1.0, насамперед 12-годинний формат (чи з'являється дп/пп і чи влазить у віконце).
- [ ] **ПК-мод жодного разу не запускався** — SMAPI не встановлений на десктопній грі. Протестувати перед публікацією.
- [ ] Зробити 2-3 скріншоти для сторінок (меню вибору мов з укр. рядком, годинник, клунки українською).
- [ ] Погодити з Pereclaw згадку пака в описі (не обов'язково, але ввічливо).
