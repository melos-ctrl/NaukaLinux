# NaukaLinux

Platforma e-learningowa stworzona do nauki systemów z rodziny Linux i administracji serwerami. Projekt umożliwia mentorom tworzenie interaktywnych kursów, a uczniom śledzenie swoich postępów w nauce.

![Logo](Frontend/src/images/tux.svg)

## Główne Funkcjonalności

**Dla Ucznia:**
* Rejestracja i logowanie oparte na tokenach JWT.
* Spersonalizowany dashboard ze śledzeniem postępów (procent ukończenia).
* Rozwiązywanie interaktywnych quizów, czytanie materiałów i nauka z fiszek.

**Dla Mentora:**
* Dedykowany Panel Mentora do zarządzania własnymi kursami.
* Interaktywny Kreator Kursów (dodawanie lekcji, quizów i tekstów).
* Podgląd statystyk zapisanych uczniów.

## Tech Stack

* **Frontend:** Vue 3 (Composition API), TypeScript, Tailwind CSS, Vue Router
* **Backend:** C# / .NET 10, ASP.NET Core Web API, Entity Framework Core
* **Baza Danych:** PostgreSQL (uruchamiany w Dockerze)
* **Zabezpieczenia:** BCrypt (haszowanie haseł), JWT (autoryzacja i autentykacja)

## Roadmapa Projektu (Plan Rozwoju)

Projekt jest aktywnie rozwijany. Poniżej znajduje się lista ukończonych oraz planowanych funkcjonalności:

### Etap 1: MVP & Zarządzanie (Zrealizowane)
- [x] Projekt bazy danych i integracja z PostgreSQL.
- [x] Autoryzacja i autentykacja (JWT, BCrypt).
- [x] Obsługa ról użytkowników (Uczeń / Mentor).
- [x] Frontend w Vue 3 z routingiem i zabezpieczeniem widoków.
- [x] Panel Mentora i interaktywny Kreator Kursów (moduły tekstu, quizów i fiszek).

### Etap 2: Nauka i Panel Ucznia (W trakcie)
- [ ] Stworzenie głównego Panelu Ucznia (Dashboard).
- [ ] Mechanizm śledzenia postępów (procent ukończenia kursu i poszczególnych lekcji).
- [ ] Widok rozwiązywania lekcji (odczyt materiałów, sprawdzanie quizów, obracane fiszki).
- [ ] Zapisywanie stanu ukończenia bloków do bazy danych (`UserProgress`).

### Etap 3: Zaawansowane Funkcje
- [ ] Integracja Wirtualnego Terminala (wykonywanie prawdziwych komend Linuxowych w przeglądarce).
- [ ] System powiadomień w czasie rzeczywistym dla uczniów i mentorów.
- [ ] Ocenianie kursów i komentarze pod lekcjami.
