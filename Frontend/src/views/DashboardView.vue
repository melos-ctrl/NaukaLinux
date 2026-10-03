<script setup lang="ts">
    import { onBeforeUnmount, onMounted, ref } from 'vue'

    type UserProfile = {
        username: string;
        avatarUrl: string | null;
    };

    // nazwa użytkownika i avartar
    const username = ref('');
    const avatarUrl = ref<string | null>(null);
    const previewUrl = ref<string | null>(null);
    const message = ref('');

    // input do plików żeby dać avatar
    const fileInput = ref<HTMLInputElement | null>(null);
    
    // funkcja do wywołania wyboru pliku
    const triggerFileInput = () => {
        fileInput.value?.click();
    };

    const loadProfile = async () => {
        const token = localStorage.getItem('token');
    
        if (!token) {
            message.value = 'Zaloguj się aby zobaczyć swój profil.';
            return;
        }

        try { 
            const response = await fetch('/api/users/me', {
                headers: {
                    'Authorization': `Bearer ${token}`,
                },
            })
        if (!response.ok) {
            throw new Error(await response.text());
        }

        const profile = ( await response.json()) as UserProfile;
        username.value = profile.username;
        avatarUrl.value = profile.avatarUrl;
        } catch (error) {
            message.value = 'Wystąpił błąd podczas ładowania profilu.';
            console.error(error);
        }
    }

    // funkcja do obsługi zmiany avatara    
    const handleAvatarChange = async (event: Event) => {
        const input = event.target as HTMLInputElement;
        const file = input.files?.[0];

        if (!file) {
            return;
        }

        // Walidacja typu pliku

        if (!['image/jpeg', 'image/png', 'image/webp'].includes(file.type)) {
            message.value = 'Nieprawidłowy format pliku. Dozwolone formaty: JPEG, PNG lub WebP.';
            input.value = ''; // Reset input
            return;
        }

        // Walidacja rozmiaru pliku
        if (file.size > 5 * 1024 * 1024) {
            message.value = 'Zdjęcie może mieć maksymalnie 5 MB.';
            input.value = ''; // Reset input
            return;
        }

        const token = localStorage.getItem('token');
        if (!token) {
            message.value = 'Zaloguj się aby zmienić avatar.';
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
                    'Authorization': `Bearer ${token}`,
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
            input.value = ''; // Reset input
        }
    };

    onMounted(() => {
       void loadProfile();
    });

    onBeforeUnmount(() => {
        if (previewUrl.value) {
            URL.revokeObjectURL(previewUrl.value);
        }
    });
</script>

<template>
    <div class="flex h-screen bg-floral-white overflow-hidden">
    <!-- Lewy sidebar -->
        <aside class="w-64 bg-charcoal-brown text-floral-white flex flex-col shrink-0 shadow-xl z-10 relative rounded-4xl border-r border-silver/20">
            <!-- Sekcja Avatara -->
            <div class = "p-8 flex flex-col items-center border-b border-silver/20 text-center">
                <!-- Avatar -->
                 <div class="group w-24 h-24 rounded-full bg-carbon-black border-4 border-spicy-paprika items-center justify-center flex text-3xl font-extrabold shadow-lg relative"  @click="triggerFileInput">
                    <!-- Profilowe jeżeli jest dodany-->
                     <img
                     v-if="previewUrl || avatarUrl"
                     :src="previewUrl || avatarUrl || undefined"
                     alt="Avatar"
                     class="w-full h-full rounded-full object-cover cursor-pointer"
                     />
                     <!-- Jeżeli nie ma profilowego to dodaj -->
                    <span v-else class="text-floral-white cursor-pointer">+</span>
                    <div class="absolute inset-0 bg-black/50 hidden group-hover:flex items-center justify-center text-white text-xs font-bold transition-all">
                        Zmień
                    </div>
                 

                 <!-- Input do plików jest ukryty -->

                <input 
                    type="file" 
                    ref="fileInput" 
                    class="hidden" 
                    accept="image/*" 
                    @change="handleAvatarChange" 
                />
                </div>
                
                <!-- Nazwa użytkownika -->
                <h2 class="mt-4 font-bold tracking-wide text-floral-white">
                    {{ username }}
                </h2>
            </div>
            <!-- Sekcja Nawigacji -->
            <nav>
                <router-link to="/dashboard" class="px-4 py-3 hover:bg-spicy-paprika rounded-lg font-medium transition-colors flex items-center gap-3 text-silver hover:text-floral-white">
                Mój Profil
                </router-link>
                <router-link to="/courses" class="px-4 py-3 hover:bg-spicy-paprika rounded-lg font-medium transition-colors flex items-center gap-3 text-silver hover:text-floral-white">
                Katalog Kursów
                </router-link>
                <router-link to="/sandbox" class="px-4 py-3 hover:bg-spicy-paprika rounded-lg font-medium transition-colors flex items-center gap-3 text-silver hover:text-floral-white">
                Piaskownica
                </router-link>
                <router-link to="/settings" class="px-4 py-3 hover:bg-spicy-paprika rounded-lg font-medium transition-colors flex items-center gap-3 text-silver hover:text-floral-white">
                Ustawienia Konta
                </router-link>

            </nav>

        </aside>
        <main class="flex-1 overflow-y-auto p-10">
        </main>
    </div>
</template>
