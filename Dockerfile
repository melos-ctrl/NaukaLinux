FROM lscr.io/linuxserver/webtop:ubuntu-xfce

# Strefa czasowa i język
ENV TZ=Europe/Warsaw \
    LANG=pl_PL.UTF-8 \
    LC_ALL=pl_PL.UTF-8

RUN apt-get update && \
    DEBIAN_FRONTEND=noninteractive apt-get install -y --no-install-recommends \
        bash-completion \
        curl \
        dnsutils \
        git \
        htop \
        iproute2 \
        iputils-ping \
        jq \
        less \
        locales \
        man-db \
        manpages \
        manpages-pl \
        nano \
        procps \
        sudo \
        tree \
        tzdata \
        unzip \
        vim \
        wget \
        zip && \
    locale-gen pl_PL.UTF-8 && \
    ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && echo $TZ > /etc/timezone && \
    rm -rf /var/lib/apt/lists/*

# --- Sudo: kursant NIE ma pełnego sudo ---
# Usuwamy wszystkie dotychczasowe uprawnienia (wpis abc, grupy %sudo i %admin),
# więc nawet jeśli skrypty startowe webtopa dodadzą abc do grupy sudo, nic to nie da.
RUN sed -i -E '/^(%sudo|%admin|abc)[[:space:]]/d' /etc/sudoers && \
    find /etc/sudoers.d -type f ! -name README -delete && \
    (gpasswd -d abc sudo 2>/dev/null || true) && \
    passwd -l root

# --- Nakładka na apt: jedyne, co kursant może zrobić przez sudo ---
# Leży w /usr/local/sbin, które jest w ścieżce sudo PRZED /usr/bin,
# więc "sudo apt install X" trafia tutaj, a nie do prawdziwego apt.
COPY <<"APT_EOF" /usr/local/sbin/apt
#!/bin/bash
# Bezpieczna nakładka na apt dla kursanta.
#  - bez sudo: przekazuje wszystko do prawdziwego apt (search, show, list...)
#  - z sudo:   pozwala tylko na: update, install/remove pakietów z listy dozwolonych
set -euo pipefail

PRAWDZIWY_APT=/usr/bin/apt
LISTA=/etc/apt-kurs/dozwolone.txt

# zwykły użytkownik - prawdziwy apt (bez roota i tak nic nie zainstaluje)
if [ "$(id -u)" -ne 0 ]; then
  exec "$PRAWDZIWY_APT" "$@"
fi

# jako root: czyste, bezpieczne środowisko (bez pagerów i okienek)
export PATH=/usr/sbin:/usr/bin:/sbin:/bin
export DEBIAN_FRONTEND=noninteractive APT_LISTCHANGES_FRONTEND=none PAGER=cat
unset APT_CONFIG

polecenie="${1:-}"
shift || true

case "$polecenie" in
  update)
    if [ $# -ne 0 ]; then echo "Użycie: sudo apt update"; exit 1; fi
    apt-get update
    ;;
  install|remove)
    pakiety=()
    for p in "$@"; do
      [ "$p" = "-y" ] && continue
      if ! [[ "$p" =~ ^[a-z0-9][a-z0-9.+-]*$ ]]; then
        echo "Niedozwolony argument: $p"; exit 1
      fi
      if ! grep -Fxq "$p" "$LISTA"; then
        echo "Pakietu '$p' nie ma na liście dozwolonych. Lista: cat $LISTA"; exit 1
      fi
      pakiety+=("$p")
    done
    if [ ${#pakiety[@]} -eq 0 ]; then echo "Użycie: sudo apt $polecenie <pakiet>"; exit 1; fi
    if [ "$polecenie" = install ]; then
      apt-get install -y --no-install-recommends -o Dpkg::Options::=--force-confold "${pakiety[@]}"
    else
      apt-get remove -y "${pakiety[@]}"
    fi
    # na wszelki wypadek: zdejmij setuid/setgid z czegokolwiek, co pojawiło się z pakietem
    find / -xdev -type f -perm /6000 ! -path /usr/bin/sudo -exec chmod ug-s {} + 2>/dev/null || true
    ;;
  *)
    echo "Przez sudo możesz użyć tylko:"
    echo "  sudo apt update"
    echo "  sudo apt install <pakiet>"
    echo "  sudo apt remove <pakiet>"
    echo "Pozostałe polecenia (apt search, apt show, apt list) wpisuj bez sudo."
    echo "Lista dozwolonych pakietów: cat $LISTA"
    exit 1
    ;;
esac
APT_EOF

# Lista pakietów, które kursant może sam zainstalować (jeden pakiet w linii)
COPY <<"EOF" /etc/apt-kurs/dozwolone.txt
btop
cmatrix
mc
EOF

# Jedyne uprawnienie sudo kursanta
COPY <<"EOF" /etc/sudoers.d/kurs-apt
abc ALL=(root) NOPASSWD: /usr/local/sbin/apt
EOF

RUN chown root:root /usr/local/sbin/apt /etc/apt-kurs/dozwolone.txt /etc/sudoers.d/kurs-apt && \
    chmod 755 /usr/local/sbin/apt && \
    chmod 644 /etc/apt-kurs/dozwolone.txt && \
    chmod 440 /etc/sudoers.d/kurs-apt && \
    visudo -c && \
    # setuid zostaje TYLKO na sudo; su, passwd, mount, pkexec itd. tracą go
    rm -f /usr/bin/pkexec && \
    find / -xdev -type f -perm /6000 ! -path /usr/bin/sudo -exec chmod ug-s {} +

# --- Narzędzia sieciowe działające BEZ sudo (tylko utrudnienie, ochrona = zapora) ---
RUN rm -f /usr/bin/nc /usr/bin/nc.openbsd /usr/bin/netcat /usr/bin/ncat \
          /usr/bin/socat /usr/bin/nmap /usr/bin/telnet /usr/bin/ftp \
          /usr/bin/ssh /usr/bin/scp /usr/bin/sftp /usr/bin/ssh-keyscan

# Terminal otwierany automatycznie po starcie sesji XFCE
COPY <<"EOF" /etc/xdg/autostart/terminal-autostart.desktop
[Desktop Entry]
Type=Application
Name=Terminal kursanta
Comment=Terminal otwierany automatycznie po zalogowaniu
Exec=sh -c "sleep 2 && exec xfce4-terminal --working-directory=$HOME"
Icon=utilities-terminal
Terminal=false
OnlyShowIn=XFCE;
X-GNOME-Autostart-enabled=true
NoDisplay=true
EOF

#Pliki do pracy kursantowi (tylko do odczytu)
COPY materialy/ /config/Desktop/Zadania/ 
