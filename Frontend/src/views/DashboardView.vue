<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue';

type UserProfile = {
    username: string;
    avatarUrl: string | null;
};

type DashboardCourse = {
    courseId: number;
    title: string;
    description: string;
    lastOpenedAt?: string | null;
};

const username = ref('');
const avatarUrl = ref<string | null>(null);
const previewUrl = ref<string | null>(null);
const message = ref('');

const enrolledCourses = ref<DashboardCourse[]>([]);
const isDashboardLoading = ref(true);
const dashboardError = ref('');

const fileInput = ref<HTMLInputElement | null>(null);

const lastOpenedCourse = computed(() => {
    let latestCourse: DashboardCourse | null = null;
    let latestOpenedAt = Number.NEGATIVE_INFINITY;

    for (const course of enrolledCourses.value) {
        const openedAt = Date.parse(course.lastOpenedAt ?? '');

        if (Number.isFinite(openedAt) && openedAt > latestOpenedAt) {
            latestCourse = course;
            latestOpenedAt = openedAt;
        }
    }

    return latestCourse;
});

const featuredCourse = computed(
    () => lastOpenedCourse.value ?? enrolledCourses.value[0] ?? null,
);

const triggerFileInput = () => {
    fileInput.value?.click();
};

const loadProfile = async () => {
    const token = localStorage.getItem('token');

    if (!token) {
        message.value = 'Zaloguj się, aby zobaczyć swój profil.';
        return;
    }

    try {
        const response = await fetch('/api/users/me', {
            headers: {
                Authorization: `Bearer ${token}`,
            },
        });

        if (!response.ok) {
            throw new Error(await response.text());
        }

        const profile = (await response.json()) as UserProfile;
        username.value = profile.username;
        avatarUrl.value = profile.avatarUrl;
        message.value = '';
    } catch (error) {
        message.value = 'Wystąpił błąd podczas ładowania profilu.';
        console.error(error);
    }
};

const handleAvatarChange = async (event: Event) => {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];

    if (!file) {
        return;
    }

    if (!['image/jpeg', 'image/png', 'image/webp'].includes(file.type)) {
        message.value = 'Dozwolone formaty zdjęcia: JPEG, PNG lub WebP.';
        input.value = '';
        return;
    }

    if (file.size > 5 * 1024 * 1024) {
        message.value = 'Zdjęcie może mieć maksymalnie 5 MB.';
        input.value = '';
        return;
    }

    const token = localStorage.getItem('token');

    if (!token) {
        message.value = 'Zaloguj się, aby zmienić avatar.';
        input.value = '';
        return;
    }

    if (previewUrl.value) {
        URL.revokeObjectURL(previewUrl.value);
    }

    previewUrl.value = URL.createObjectURL(file);
    message.value = '';

    const formData = new FormData();
    formData.append('file', file);

    try {
        const response = await fetch('/api/users/me/avatar', {
            method: 'POST',
            headers: {
                Authorization: `Bearer ${token}`,
            },
            body: formData,
        });

        if (!response.ok) {
            throw new Error(await response.text());
        }

        const result = (await response.json()) as { avatarUrl: string };
        avatarUrl.value = result.avatarUrl;
        message.value = 'Avatar został pomyślnie zaktualizowany.';
    } catch (error) {
        message.value = 'Wystąpił błąd podczas aktualizacji avatara.';
        console.error(error);
    } finally {
        if (previewUrl.value) {
            URL.revokeObjectURL(previewUrl.value);
            previewUrl.value = null;
        }

        input.value = '';
    }
};

const loadDashboard = async () => {
    isDashboardLoading.value = true;
    dashboardError.value = '';

    const token = localStorage.getItem('token');

    if (!token) {
        dashboardError.value = 'Zaloguj się, aby zobaczyć swoje kursy.';
        isDashboardLoading.value = false;
        return;
    }

    try {
        const response = await fetch('/api/users/me/courses', {
            headers: {
                Authorization: `Bearer ${token}`,
            },
        });

        if (!response.ok) {
            throw new Error(await response.text());
        }

        enrolledCourses.value = (await response.json()) as DashboardCourse[];
        dashboardError.value = '';
    } catch (error) {
        dashboardError.value = 'Wystąpił błąd podczas ładowania kursów.';
        console.error(error);
    } finally {
        isDashboardLoading.value = false;
    }
};

onMounted(() => {
    void loadProfile();
    void loadDashboard();
});

onBeforeUnmount(() => {
    if (previewUrl.value) {
        URL.revokeObjectURL(previewUrl.value);
    }
});
</script>

<template>
    <div class="-m-4 flex min-h-0 flex-1 flex-col gap-4 md:h-full md:flex-row md:gap-4">
        <aside
            class="flex w-full shrink-0 flex-col rounded-2xl border border-silver/20 bg-charcoal-brown text-floral-white shadow-lg md:w-64 m-2"
        >
            <section class="flex flex-col items-center border-b border-silver/20 p-6 text-center">
                <button
                    type="button"
                    class="group relative flex h-24 w-24 items-center justify-center overflow-hidden rounded-full border-4 border-spicy-paprika bg-carbon-black text-3xl font-extrabold shadow-lg focus-visible:outline-2 focus-visible:outline-offset-4 focus-visible:outline-spicy-paprika"
                    aria-label="Zmień avatar"
                    @click="triggerFileInput"
                >
                    <img
                        v-if="previewUrl || avatarUrl"
                        :src="previewUrl || avatarUrl || undefined"
                        :alt="`Avatar użytkownika ${username}`"
                        class="h-full w-full object-cover"
                    />
                    <span v-else class="text-floral-white">+</span>
                    <span
                        class="absolute inset-0 hidden items-center justify-center bg-black/55 text-xs font-bold text-white group-hover:flex group-focus-visible:flex"
                    >
                        Zmień
                    </span>
                </button>

                <input
                    ref="fileInput"
                    type="file"
                    class="hidden"
                    accept="image/jpeg,image/png,image/webp"
                    @change="handleAvatarChange"
                />

                <h2 class="mt-4 break-all font-bold tracking-wide">
                    {{ username || 'Twój profil' }}
                </h2>
                <p
                    v-if="message"
                    class="mt-3 text-sm text-silver"
                    role="status"
                    aria-live="polite"
                >
                    {{ message }}
                </p>
                <p v-else class="mt-1 text-sm text-silver">
                    Kliknij avatar, aby go zmienić
                </p>
            </section>

            <nav class="flex flex-col gap-1 p-3" aria-label="Nawigacja panelu użytkownika">
                <router-link
                    to="/dashboard"
                    class="rounded-lg px-4 py-3 font-medium text-silver transition-colors hover:bg-spicy-paprika hover:text-floral-white"
                    active-class="bg-spicy-paprika text-floral-white"
                >
                    Panel ucznia
                </router-link>
                <router-link
                    to="/courses"
                    class="rounded-lg px-4 py-3 font-medium text-silver transition-colors hover:bg-spicy-paprika hover:text-floral-white"
                    active-class="bg-spicy-paprika text-floral-white"
                >
                    Katalog kursów
                </router-link>
                <router-link
                    to="/sandbox"
                    class="rounded-lg px-4 py-3 font-medium text-silver transition-colors hover:bg-spicy-paprika hover:text-floral-white"
                    active-class="bg-spicy-paprika text-floral-white"
                >
                    Piaskownica
                </router-link>
                <router-link
                    to="/settings"
                    class="rounded-lg px-4 py-3 font-medium text-silver transition-colors hover:bg-spicy-paprika hover:text-floral-white"
                    active-class="bg-spicy-paprika text-floral-white"
                >
                    Ustawienia
                </router-link>
            </nav>
        </aside>

        <main class="min-h-0 min-w-0 flex-1 space-y-8 overflow-y-auto p-5 md:p-8 lg:p-10">
            <header>
                <p class="text-sm font-semibold uppercase tracking-wider text-spicy-paprika">
                    Panel ucznia
                </p>
                <h1 class="mt-1 text-3xl font-bold text-charcoal-brown">
                    Cześć<span v-if="username">, {{ username }}</span>!
                </h1>
                <p class="mt-2 text-charcoal-brown/70">
                    Wróć do nauki lub znajdź kurs, który Cię interesuje.
                </p>
            </header>

            <section
                class="overflow-hidden rounded-2xl bg-charcoal-brown p-6 text-floral-white shadow-md md:p-8"
            >
                <p class="text-sm font-semibold uppercase tracking-wider text-silver">
                    {{ lastOpenedCourse ? 'Kontynuuj naukę' : 'Twoja nauka' }}
                </p>

                <template v-if="isDashboardLoading">
                    <div
                        class="mt-5 h-7 w-2/3 animate-pulse rounded bg-floral-white/15"
                        aria-hidden="true"
                    />
                    <div
                        class="mt-3 h-4 w-full max-w-lg animate-pulse rounded bg-floral-white/10"
                        aria-hidden="true"
                    />
                    <p class="sr-only" role="status">Ładowanie kursów...</p>
                </template>

                <div
                    v-else-if="dashboardError"
                    class="mt-4"
                    role="alert"
                >
                    <h2 class="text-xl font-bold">Nie udało się załadować kursów</h2>
                    <p class="mt-2 text-silver">{{ dashboardError }}</p>
                    <button
                        type="button"
                        class="mt-5 rounded-lg bg-spicy-paprika px-5 py-3 font-semibold text-white transition-colors hover:opacity-90 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-white"
                        @click="loadDashboard"
                    >
                        Spróbuj ponownie
                    </button>
                </div>

                <template v-else-if="featuredCourse">
                    <h2 class="mt-2 text-2xl font-bold">
                        {{ featuredCourse.title }}
                    </h2>
                    <p class="mt-2 max-w-2xl text-silver">
                        {{
                            featuredCourse.description ||
                            'Otwórz kurs i ucz się dalej.'
                        }}
                    </p>
                    <router-link
                        :to="`/course/${featuredCourse.courseId}`"
                        class="mt-6 inline-flex rounded-lg bg-spicy-paprika px-5 py-3 font-semibold text-white transition-colors hover:opacity-90 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-white"
                    >
                        {{ lastOpenedCourse ? 'Wróć do kursu' : 'Rozpocznij kurs' }}
                    </router-link>
                </template>

                <template v-else-if="!dashboardError">
                    <h2 class="mt-2 text-2xl font-bold">
                        Wybierz kurs na początek
                    </h2>
                    <p class="mt-2 text-silver">
                        Przejrzyj katalog i zapisz się na kurs, który Cię interesuje.
                    </p>
                    <router-link
                        to="/courses"
                        class="mt-6 inline-flex rounded-lg bg-spicy-paprika px-5 py-3 font-semibold text-white transition-colors hover:opacity-90 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-white"
                    >
                        Przeglądaj kursy
                    </router-link>
                </template>
            </section>

            <section>
                <div class="mb-4 flex flex-wrap items-center justify-between gap-3">
                    <div>
                        <p class="text-sm font-medium text-silver">Twój postęp</p>
                        <h2 class="text-xl font-bold text-charcoal-brown">
                            Kursy, w których się uczę
                        </h2>
                    </div>
                    <router-link
                        to="/courses"
                        class="rounded-lg px-3 py-2 text-sm font-semibold text-spicy-paprika transition-colors hover:bg-spicy-paprika/10 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-spicy-paprika"
                    >
                        Katalog kursów
                        <span aria-hidden="true">→</span>
                    </router-link>
                </div>

                <p
                    v-if="!isDashboardLoading && !dashboardError && enrolledCourses.length === 0"
                    class="rounded-xl border border-silver/50 bg-white p-6 text-charcoal-brown/70 shadow-sm"
                >
                    Nie masz jeszcze zapisanych kursów.
                    <router-link
                        to="/courses"
                        class="ml-1 font-semibold text-spicy-paprika hover:underline"
                    >
                        Znajdź kurs
                    </router-link>
                </p>

                <div
                    v-else-if="!isDashboardLoading && !dashboardError"
                    class="grid gap-5 sm:grid-cols-2 xl:grid-cols-3"
                >
                    <article
                        v-for="course in enrolledCourses"
                        :key="course.courseId"
                        class="flex min-h-52 flex-col rounded-xl border border-silver/50 bg-white p-5 shadow-sm transition-shadow hover:shadow-md"
                    >
                        <span
                            class="mb-3 w-fit rounded-full bg-spicy-paprika/10 px-3 py-1 text-xs font-semibold text-spicy-paprika"
                        >
                            Kurs
                        </span>
                        <h3 class="text-lg font-bold text-charcoal-brown">
                            {{ course.title }}
                        </h3>
                        <p class="mt-2 flex-1 text-sm leading-relaxed text-charcoal-brown/70">
                            {{ course.description || 'Brak opisu kursu.' }}
                        </p>
                        <router-link
                            :to="`/course/${course.courseId}`"
                            class="mt-5 inline-flex w-full justify-center rounded-lg bg-charcoal-brown px-4 py-2.5 font-medium text-white transition-colors hover:bg-spicy-paprika focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-spicy-paprika"
                        >
                            Otwórz kurs
                        </router-link>
                    </article>
                </div>

                <div
                    v-else-if="isDashboardLoading"
                    class="grid gap-5 sm:grid-cols-2 xl:grid-cols-3"
                    aria-hidden="true"
                >
                    <div
                        v-for="item in 3"
                        :key="item"
                        class="h-52 animate-pulse rounded-xl border border-silver/30 bg-white"
                    />
                </div>
            </section>
        </main>
    </div>
</template>
