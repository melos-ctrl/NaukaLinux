<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue';
import SidebarLayout from '@/components/SidebarLayout.vue';
import StudentNav from '@/components/StudentNav.vue';
import PageHeader from '@/components/PageHeader.vue';

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
    <SidebarLayout>
        <template #profile>
            <button
                type="button"
                class="group relative flex h-24 w-24 items-center justify-center overflow-hidden rounded-full border-4 border-spicy-paprika bg-carbon-black text-3xl font-bold shadow-lg focus-visible:outline-2 focus-visible:outline-offset-4 focus-visible:outline-spicy-paprika"
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

            <h2 class="mt-2 break-all font-bold">
                {{ username || 'Twój profil' }}
            </h2>
            <p
                v-if="message"
                class="text-sm text-silver"
                role="status"
                aria-live="polite"
            >
                {{ message }}
            </p>
            <p v-else class="text-sm text-silver">
                Kliknij avatar, aby go zmienić
            </p>
        </template>

        <template #nav>
            <StudentNav />
        </template>

        <PageHeader
            eyebrow="Panel ucznia"
            :title="username ? `Cześć, ${username}!` : 'Cześć!'"
            lead="Wróć do nauki lub znajdź kurs, który Cię interesuje."
        />

        <section class="card-dark">
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
                    class="btn btn-primary mt-5"
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
                    class="btn btn-primary mt-6"
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
                    class="btn btn-primary mt-6"
                >
                    Przeglądaj kursy
                </router-link>
            </template>
        </section>

        <section>
            <div class="mb-4 flex flex-wrap items-end justify-between gap-3">
                <div>
                    <p class="text-sm text-muted">Twój postęp</p>
                    <h2 class="section-title">
                        Kursy, w których się uczę
                    </h2>
                </div>
                <router-link
                    to="/courses"
                    class="btn btn-ghost"
                >
                    Katalog kursów
                    <span aria-hidden="true">→</span>
                </router-link>
            </div>

            <p
                v-if="!isDashboardLoading && !dashboardError && enrolledCourses.length === 0"
                class="empty-state"
            >
                Nie masz jeszcze zapisanych kursów.
                <router-link
                    to="/courses"
                    class="ml-1 font-semibold text-paprika-600 hover:underline"
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
                    class="card card-hover flex min-h-52 flex-col md:p-5"
                >
                    <span class="badge badge-primary mb-3">Kurs</span>
                    <h3 class="text-lg font-bold">
                        {{ course.title }}
                    </h3>
                    <p class="mt-2 flex-1 text-sm leading-relaxed text-muted">
                        {{ course.description || 'Brak opisu kursu.' }}
                    </p>
                    <router-link
                        :to="`/course/${course.courseId}`"
                        class="btn btn-dark mt-5 w-full"
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
                    class="h-52 animate-pulse rounded-2xl border border-line bg-white"
                />
            </div>
        </section>
    </SidebarLayout>
</template>
