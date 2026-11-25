<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { useRouter } from 'vue-router';
import SidebarLayout from '@/components/SidebarLayout.vue';
import StudentNav from '@/components/StudentNav.vue';
import PageHeader from '@/components/PageHeader.vue';

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
    <SidebarLayout>
        <template #nav>
            <StudentNav />
        </template>

        <div class="max-w-3xl space-y-8">
            <PageHeader
                eyebrow="Konto"
                title="Ustawienia"
                lead="Zarządzaj danymi i bezpieczeństwem swojego konta."
            />

            <section class="card">
                <div class="mb-6">
                    <h2 class="section-title">Dane konta</h2>
                    <p class="mt-1 text-sm text-muted">
                        Podstawowe informacje przypisane do Twojego konta.
                    </p>
                </div>

                <form class="space-y-5" @submit.prevent="saveAccount">
                    <div>
                        <label for="username" class="label">Nazwa użytkownika</label>
                        <input
                            id="username"
                            name="username"
                            type="text"
                            v-model.trim="username"
                            minlength="3"
                            maxlength="100"
                            required
                            :disabled="isLoadingAccount || isSavingAccount"
                            class="input"
                        />
                    </div>

                    <div>
                        <label for="email" class="label">Adres e-mail</label>
                        <input
                            id="email"
                            name="email"
                            type="email"
                            v-model.trim="email"
                            maxlength="254"
                            required
                            :disabled="isLoadingAccount || isSavingAccount"
                            class="input"
                        />
                    </div>

                    <p
                        v-if="accountMessage"
                        class="alert"
                        :class="isAccountMessageError ? 'alert-error' : 'alert-success'"
                        role="status"
                        aria-live="polite"
                    >
                        {{ accountMessage }}
                    </p>

                    <button
                        type="submit"
                        :disabled="isLoadingAccount || isSavingAccount"
                        class="btn btn-dark w-full sm:w-auto"
                    >
                        {{ isLoadingAccount ? 'Ładowanie...' : isSavingAccount ? 'Zapisywanie...' : 'Zapisz zmiany' }}
                    </button>
                </form>
            </section>

            <section class="card">
                <div class="flex flex-col justify-between gap-4 sm:flex-row sm:items-center">
                    <div>
                        <h2 class="section-title">Bezpieczeństwo</h2>
                        <p class="mt-1 text-sm text-muted">
                            Zmieniaj hasło regularnie, aby lepiej chronić konto.
                        </p>
                    </div>
                    <button
                        type="button"
                        class="btn btn-outline shrink-0"
                        :aria-expanded="isPasswordFormOpen"
                        @click="isPasswordFormOpen = !isPasswordFormOpen"
                    >
                        {{ isPasswordFormOpen ? 'Anuluj zmianę hasła' : 'Zmień hasło' }}
                    </button>
                </div>

                <form
                    v-if="isPasswordFormOpen"
                    class="mt-6 space-y-5 border-t border-line pt-6"
                    @submit.prevent="changePassword"
                >
                    <div>
                        <label for="current-password" class="label">Aktualne hasło</label>
                        <input
                            id="current-password"
                            v-model="currentPassword"
                            type="password"
                            autocomplete="current-password"
                            required
                            class="input"
                        />
                    </div>

                    <div>
                        <label for="new-password" class="label">Nowe hasło</label>
                        <input
                            id="new-password"
                            v-model="newPassword"
                            type="password"
                            autocomplete="new-password"
                            minlength="8"
                            required
                            class="input"
                        />
                    </div>

                    <div>
                        <label for="confirm-password" class="label">Powtórz nowe hasło</label>
                        <input
                            id="confirm-password"
                            v-model="confirmPassword"
                            type="password"
                            autocomplete="new-password"
                            minlength="8"
                            required
                            class="input"
                        />
                    </div>

                    <button
                        type="submit"
                        :disabled="isSavingPassword"
                        class="btn btn-primary w-full sm:w-auto"
                    >
                        {{ isSavingPassword ? 'Zapisywanie...' : 'Zapisz nowe hasło' }}
                    </button>
                </form>

                <!-- poza formularzem, żeby komunikat "Hasło zostało zmienione" był widoczny po jego zamknięciu -->
                <p
                    v-if="passwordMessage"
                    class="alert alert-info mt-5"
                    role="status"
                    aria-live="polite"
                >
                    {{ passwordMessage }}
                </p>
            </section>

            <section class="card border-red-200">
                <div class="flex flex-col justify-between gap-4 sm:flex-row sm:items-center">
                    <div>
                        <h2 class="section-title">Usuń konto</h2>
                        <p class="mt-1 text-sm text-muted">
                            Ta czynność jest nieodwracalna.
                        </p>
                    </div>
                    <button
                        type="button"
                        class="btn btn-danger shrink-0"
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
                        <label for="delete-password" class="label">Potwierdź hasłem</label>
                        <input
                            id="delete-password"
                            v-model="deletePassword"
                            type="password"
                            autocomplete="current-password"
                            required
                            class="input focus:border-red-700 focus:ring-red-700/20"
                        />
                    </div>

                    <p
                        v-if="deleteMessage"
                        class="alert alert-error"
                        role="alert"
                    >
                        {{ deleteMessage }}
                    </p>

                    <button
                        type="submit"
                        :disabled="isDeletingAccount"
                        class="btn btn-danger w-full sm:w-auto"
                    >
                        {{ isDeletingAccount ? 'Usuwanie...' : 'Potwierdź usunięcie konta' }}
                    </button>
                </form>
            </section>
        </div>
    </SidebarLayout>
</template>
