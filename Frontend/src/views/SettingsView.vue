<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { useRouter } from 'vue-router';

type AccountProfile = {
    username: string;
    email: string;
};

const router = useRouter();
const username = ref('');
const email = ref('');
const isLoadingAccount = ref(true);
const isSavingAccount = ref(false);
const accountMessage = ref('');
const isAccountMessageError = ref(false);

const isPasswordFormOpen = ref(false);
const currentPassword = ref('');
const newPassword = ref('');
const confirmPassword = ref('');
const passwordMessage = ref('');
const isSavingPassword = ref(false);

const isDeleteFormOpen = ref(false);
const deletePassword = ref('');
const deleteMessage = ref('');
const isDeletingAccount = ref(false);

const getToken = () => localStorage.getItem('token');

const readErrorMessage = async (response: Response, fallback: string) => {
    const body = await response.text();
    return body || fallback;
};

const loadAccount = async () => {
    const token = getToken();

    if (!token) {
        accountMessage.value = 'Zaloguj się ponownie, aby zarządzać kontem.';
        isAccountMessageError.value = true;
        isLoadingAccount.value = false;
        return;
    }

    try {
        const response = await fetch('/api/users/me', {
            headers: {
                Authorization: `Bearer ${token}`,
            },
        });

        if (!response.ok) {
            throw new Error(await readErrorMessage(response, 'Nie udało się pobrać danych konta.'));
        }

        const profile = (await response.json()) as AccountProfile;
        username.value = profile.username;
        email.value = profile.email;
        accountMessage.value = '';
    } catch (error) {
        accountMessage.value =
            error instanceof Error ? error.message : 'Wystąpił błąd podczas ładowania konta.';
        isAccountMessageError.value = true;
    } finally {
        isLoadingAccount.value = false;
    }
};

const saveAccount = async () => {
    accountMessage.value = '';
    isAccountMessageError.value = false;

    const token = getToken();
    if (!token) {
        accountMessage.value = 'Zaloguj się ponownie, aby zapisać zmiany.';
        isAccountMessageError.value = true;
        return;
    }

    isSavingAccount.value = true;

    try {
        const response = await fetch('/api/users/me', {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
                Authorization: `Bearer ${token}`,
            },
            body: JSON.stringify({
                username: username.value,
                email: email.value,
            }),
        });

        if (!response.ok) {
            throw new Error(await readErrorMessage(response, 'Nie udało się zapisać zmian.'));
        }

        const profile = (await response.json()) as AccountProfile;
        username.value = profile.username;
        email.value = profile.email;
        accountMessage.value = 'Dane konta zostały zapisane.';
    } catch (error) {
        accountMessage.value =
            error instanceof Error ? error.message : 'Wystąpił błąd podczas zapisywania konta.';
        isAccountMessageError.value = true;
    } finally {
        isSavingAccount.value = false;
    }
};

const changePassword = async () => {
    passwordMessage.value = '';

    if (newPassword.value !== confirmPassword.value) {
        passwordMessage.value = 'Hasła nie są zgodne.';
        return;
    }

    if (newPassword.value.length < 8) {
        passwordMessage.value = 'Hasło musi mieć co najmniej 8 znaków.';
        return;
    }

    const token = getToken();
    if (!token) {
        passwordMessage.value = 'Zaloguj się ponownie, aby zmienić hasło.';
        return;
    }

    isSavingPassword.value = true;

    try {
        const response = await fetch('/api/users/me/password', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                Authorization: `Bearer ${token}`,
            },
            body: JSON.stringify({
                currentPassword: currentPassword.value,
                newPassword: newPassword.value,
            }),
        });

        if (!response.ok) {
            throw new Error(await readErrorMessage(response, 'Nie udało się zmienić hasła.'));
        }

        passwordMessage.value = 'Hasło zostało zmienione.';
        currentPassword.value = '';
        newPassword.value = '';
        confirmPassword.value = '';
        isPasswordFormOpen.value = false;
    } catch (error) {
        passwordMessage.value =
            error instanceof Error ? error.message : 'Wystąpił błąd podczas zmiany hasła.';
    } finally {
        isSavingPassword.value = false;
    }
};

const deleteAccount = async () => {
    deleteMessage.value = '';

    const token = getToken();
    if (!token) {
        deleteMessage.value = 'Zaloguj się ponownie, aby usunąć konto.';
        return;
    }

    isDeletingAccount.value = true;

    try {
        const response = await fetch('/api/users/me', {
            method: 'DELETE',
            headers: {
                'Content-Type': 'application/json',
                Authorization: `Bearer ${token}`,
            },
            body: JSON.stringify({ password: deletePassword.value }),
        });

        if (!response.ok) {
            throw new Error(await readErrorMessage(response, 'Nie udało się usunąć konta.'));
        }

        localStorage.removeItem('token');
        localStorage.removeItem('role');
        localStorage.removeItem('activeContainerId');
        localStorage.removeItem('desktopUrl');
        await router.replace('/login');
    } catch (error) {
        deleteMessage.value =
            error instanceof Error ? error.message : 'Wystąpił błąd podczas usuwania konta.';
    } finally {
        isDeletingAccount.value = false;
    }
};

onMounted(() => {
    void loadAccount();
});
</script>

<template>
    <div class="-m-4 min-h-full flex-1 bg-floral-white p-5 md:p-8 lg:p-10">
        <div class="mx-auto w-full max-w-4xl space-y-8">
            <header>
                <p class="text-sm font-semibold uppercase tracking-wider text-spicy-paprika">
                    Konto
                </p>
                <h1 class="mt-1 text-3xl font-bold text-charcoal-brown">
                    Ustawienia
                </h1>
                <p class="mt-2 text-charcoal-brown/70">
                    Zarządzaj danymi i bezpieczeństwem swojego konta.
                </p>
            </header>

            <section class="rounded-2xl border border-silver/50 bg-white p-5 shadow-sm md:p-7">
                <div class="mb-6">
                    <h2 class="text-xl font-bold text-charcoal-brown">Dane konta</h2>
                    <p class="mt-1 text-sm text-charcoal-brown/70">
                        Podstawowe informacje przypisane do Twojego konta.
                    </p>
                </div>

                <form class="space-y-5" @submit.prevent="saveAccount">
                    <div>
                        <label
                            for="username"
                            class="mb-2 block text-sm font-semibold text-charcoal-brown"
                        >
                            Nazwa użytkownika
                        </label>
                        <input
                            id="username"
                            name="username"
                            type="text"
                            v-model.trim="username"
                            minlength="3"
                            maxlength="100"
                            required
                            :disabled="isLoadingAccount || isSavingAccount"
                            class="w-full rounded-lg border border-silver bg-floral-white px-4 py-3 text-charcoal-brown outline-none transition focus:border-spicy-paprika focus:ring-2 focus:ring-spicy-paprika/20"
                        />
                    </div>

                    <div>
                        <label
                            for="email"
                            class="mb-2 block text-sm font-semibold text-charcoal-brown"
                        >
                            Adres e-mail
                        </label>
                        <input
                            id="email"
                            name="email"
                            type="email"
                            v-model.trim="email"
                            maxlength="254"
                            required
                            :disabled="isLoadingAccount || isSavingAccount"
                            class="w-full rounded-lg border border-silver bg-floral-white px-4 py-3 text-charcoal-brown outline-none transition focus:border-spicy-paprika focus:ring-2 focus:ring-spicy-paprika/20"
                        />
                    </div>

                    <p
                        v-if="accountMessage"
                        class="rounded-lg border px-4 py-3 text-sm"
                        :class="isAccountMessageError
                            ? 'border-red-200 bg-red-50 text-red-700'
                            : 'border-silver/50 bg-floral-white text-charcoal-brown'"
                        role="status"
                        aria-live="polite"
                    >
                        {{ accountMessage }}
                    </p>

                    <button
                        type="submit"
                        :disabled="isLoadingAccount || isSavingAccount"
                        class="inline-flex w-full items-center justify-center rounded-lg bg-charcoal-brown px-5 py-3 font-semibold text-white transition-colors hover:bg-spicy-paprika focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-spicy-paprika disabled:cursor-not-allowed disabled:opacity-60 sm:w-auto"
                    >
                        {{ isLoadingAccount ? 'Ładowanie...' : isSavingAccount ? 'Zapisywanie...' : 'Zapisz zmiany' }}
                    </button>
                </form>
            </section>

            <section class="rounded-2xl border border-silver/50 bg-white p-5 shadow-sm md:p-7">
                <div class="flex flex-col justify-between gap-4 sm:flex-row sm:items-center">
                    <div>
                        <h2 class="text-xl font-bold text-charcoal-brown">Bezpieczeństwo</h2>
                        <p class="mt-1 text-sm text-charcoal-brown/70">
                            Zmieniaj hasło regularnie, aby lepiej chronić konto.
                        </p>
                    </div>
                    <button
                        type="button"
                        class="inline-flex shrink-0 items-center justify-center rounded-lg border border-silver px-4 py-2.5 font-semibold text-charcoal-brown transition-colors hover:border-spicy-paprika hover:bg-spicy-paprika hover:text-white focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-spicy-paprika"
                        :aria-expanded="isPasswordFormOpen"
                        @click="isPasswordFormOpen = !isPasswordFormOpen"
                    >
                        {{ isPasswordFormOpen ? 'Anuluj zmianę hasła' : 'Zmień hasło' }}
                    </button>
                </div>

                <form
                    v-if="isPasswordFormOpen"
                    class="mt-6 space-y-5 border-t border-silver/40 pt-6"
                    @submit.prevent="changePassword"
                >
                    <div>
                        <label
                            for="current-password"
                            class="mb-2 block text-sm font-semibold text-charcoal-brown"
                        >
                            Aktualne hasło
                        </label>
                        <input
                            id="current-password"
                            v-model="currentPassword"
                            type="password"
                            autocomplete="current-password"
                            required
                            class="w-full rounded-lg border border-silver bg-floral-white px-4 py-3 text-charcoal-brown outline-none transition focus:border-spicy-paprika focus:ring-2 focus:ring-spicy-paprika/20"
                        />
                    </div>

                    <div>
                        <label
                            for="new-password"
                            class="mb-2 block text-sm font-semibold text-charcoal-brown"
                        >
                            Nowe hasło
                        </label>
                        <input
                            id="new-password"
                            v-model="newPassword"
                            type="password"
                            autocomplete="new-password"
                            minlength="8"
                            required
                            class="w-full rounded-lg border border-silver bg-floral-white px-4 py-3 text-charcoal-brown outline-none transition focus:border-spicy-paprika focus:ring-2 focus:ring-spicy-paprika/20"
                        />
                    </div>

                    <div>
                        <label
                            for="confirm-password"
                            class="mb-2 block text-sm font-semibold text-charcoal-brown"
                        >
                            Powtórz nowe hasło
                        </label>
                        <input
                            id="confirm-password"
                            v-model="confirmPassword"
                            type="password"
                            autocomplete="new-password"
                            minlength="8"
                            required
                            class="w-full rounded-lg border border-silver bg-floral-white px-4 py-3 text-charcoal-brown outline-none transition focus:border-spicy-paprika focus:ring-2 focus:ring-spicy-paprika/20"
                        />
                    </div>

                    <p
                        v-if="passwordMessage"
                        class="rounded-lg border border-silver/50 bg-floral-white px-4 py-3 text-sm text-charcoal-brown"
                        role="status"
                        aria-live="polite"
                    >
                        {{ passwordMessage }}
                    </p>

                    <button
                        type="submit"
                        :disabled="isSavingPassword"
                        class="inline-flex w-full items-center justify-center rounded-lg bg-spicy-paprika px-5 py-3 font-semibold text-white transition-colors hover:opacity-90 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-spicy-paprika disabled:cursor-not-allowed disabled:opacity-60 sm:w-auto"
                    >
                        {{ isSavingPassword ? 'Zapisywanie...' : 'Zapisz nowe hasło' }}
                    </button>
                </form>
            </section>

            <section
                class="rounded-2xl border border-red-200 bg-white p-5 shadow-sm md:p-7"
            >
                <div class="flex flex-col justify-between gap-4 sm:flex-row sm:items-center">
                    <div>
                        <h2 class="text-xl font-bold text-charcoal-brown">Usuń konto</h2>
                        <p class="mt-1 text-sm text-charcoal-brown/70">
                            Ta czynność jest nieodwracalna.
                        </p>
                    </div>
                    <button
                        type="button"
                        class="inline-flex shrink-0 items-center justify-center rounded-lg border border-red-300 px-4 py-2.5 font-semibold text-red-700 transition-colors hover:bg-red-600 hover:text-white focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-red-600"
                        :aria-expanded="isDeleteFormOpen"
                        @click="isDeleteFormOpen = !isDeleteFormOpen; deleteMessage = ''; deletePassword = ''"
                    >
                        {{ isDeleteFormOpen ? 'Anuluj' : 'Usuń konto' }}
                    </button>
                </div>

                <form
                    v-if="isDeleteFormOpen"
                    class="mt-6 space-y-5 border-t border-red-100 pt-6"
                    @submit.prevent="deleteAccount"
                >
                    <div>
                        <label
                            for="delete-password"
                            class="mb-2 block text-sm font-semibold text-charcoal-brown"
                        >
                            Potwierdź hasłem
                        </label>
                        <input
                            id="delete-password"
                            v-model="deletePassword"
                            type="password"
                            autocomplete="current-password"
                            required
                            class="w-full rounded-lg border border-silver bg-floral-white px-4 py-3 text-charcoal-brown outline-none transition focus:border-red-500 focus:ring-2 focus:ring-red-500/20"
                        />
                    </div>

                    <p
                        v-if="deleteMessage"
                        class="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700"
                        role="alert"
                    >
                        {{ deleteMessage }}
                    </p>

                    <button
                        type="submit"
                        :disabled="isDeletingAccount"
                        class="inline-flex w-full items-center justify-center rounded-lg bg-red-600 px-5 py-3 font-semibold text-white transition-colors hover:bg-red-700 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-red-600 disabled:cursor-not-allowed disabled:opacity-60 sm:w-auto"
                    >
                        {{ isDeletingAccount ? 'Usuwanie...' : 'Potwierdź usunięcie konta' }}
                    </button>
                </form>
            </section>
        </div>
    </div>
</template>