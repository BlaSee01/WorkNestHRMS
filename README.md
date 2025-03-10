# WorkNestHRMS
## Backend
### .NET 8.0, C#
HRM system with basic features (HR, workplace/employee management). Backend layer, with JWT authentication.

## Wymagania

Aby uruchomić projekt lokalnie, musisz mieć zainstalowane:

- środowisko deweloperskie uruchamiające projekt ASP.NET REST API Core (zalecane Microsoft Visual Studio) 
- pakiety ze środowiskiem uruchomieniowym dla platformy .NET 8.0 (jeśli wybrano środowisko inne niż zalecane)
- bazę danych PostgreSQL 16.6 oraz wybrany DBMS wspierający PostgreSQL 16.6

## Instalacja

Aby uruchomić projekt lokalnie, wykonaj poniższe kroki:

### 1. Sklonuj repozytorium

```bash
git clone https://github.com/BlaSee01/WorkNestHRMS.git
```

### 2. Przejdź do katalogu projektu
```bash
cd WorkNestHRMS
```
lub przez GUI

### 3. Otwórz projekt przy użyciu Visual Studio lub innego środowiska deweloperskiego i edytuj plik appsettings.json w celu ustawienia dostępu do własnej bazy danych.
```bash
"ConnectionStrings": {
 "DefaultConnection":
"Host=localhost;Port=5432;Database=nazwabazy;Username=postgres;Passwor
d=MyPassword123"
}
```

### 4. Zainstaluj w Powershell Microsoft Entity Framework podaną komendą.
```bash
dotnet tool install --global dotnet-ef
```

### 5. Zaktualizuj wcześniej utworzoną przez siebię bazę danych podaną komendą.
```bash
dotnet ef database update
```

### 6. Uruchom porgram podaną komendą.
```bash
dotnet run
```

<ins>Program warstwy backend uruchamiany jest na porcie ustalonym w pliku konfiguracyjnym. W celu zmiany portu, wprowadzić zmianę w kodzie źródłowym pliku launchSettings.json.</ins>
