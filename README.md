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
