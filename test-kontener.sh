#!/usr/bin/env bash
# Test limitu maszyn: zakłada N kont testowych i uruchamia każdemu pulpit.
API="http://localhost:5042/api"
N=6
PASS="TestHaslo1234"           # spełnia politykę: 12+ znaków, duża, mała litera, cyfra
CAPTCHA="XXXX.DUMMY.TOKEN.XXXX" # testowy token Turnstile

echo "== 1. Rejestracja kont =="
for i in $(seq 1 $N); do
  curl -s -o /dev/null -w "test$i: %{http_code}\n" -X POST "$API/auth/register" \
    -H "Content-Type: application/json" \
    -d "{\"email\":\"test$i@example.com\",\"username\":\"test$i\",\"password\":\"$PASS\",\"cfToken\":\"$CAPTCHA\"}"
  sleep 1   # AuthController ma rate limiting
done

echo "== 2. Potwierdzenie e-maili w bazie =="
docker exec NaukaLinux psql -U admin -d NaukaLinux -c \
  "UPDATE \"Users\" SET \"IsEmailVerified\" = true WHERE \"Username\" LIKE 'test%';"

echo "== 3. Logowanie i start pulpitów =="
for i in $(seq 1 $N); do
  TOKEN=$(curl -s -X POST "$API/auth/login" \
    -H "Content-Type: application/json" \
    -d "{\"username\":\"test$i\",\"password\":\"$PASS\",\"cfToken\":\"$CAPTCHA\"}" \
    | grep -o '"token":"[^"]*"' | cut -d'"' -f4)

  if [ -z "$TOKEN" ]; then echo "test$i: logowanie NIEUDANE"; continue; fi
  echo "$TOKEN" > "/tmp/token-test$i"

  echo -n "test$i: "
  curl -s -w "  [HTTP %{http_code}]\n" -X POST "$API/virtualdesktop/start" \
    -H "Authorization: Bearer $TOKEN"
  sleep 1
done

echo "== 4. Kontenery =="
docker ps --filter name=lab-uczen --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"