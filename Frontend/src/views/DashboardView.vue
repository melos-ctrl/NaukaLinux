<script lang="ts">
</script>

<template>
    <div class="flex h-screen bg-floral-white overflow-hidden">
    <!-- Lewy sidebar -->
        <aside class="w-96 bg-charcoal-brown text-froral-white flex flex-col shrink-0 shadow-xl z-10 relative">
            <!-- Sekcja Avatara -->
            <div class = "p-8 flex flex-col items-center border-b border-silver/20 text-center">
                <!-- Avatar -->
                 <div class="w-28 h-28 rounded-full bg-carbon-black border-4 border-spicy-paprika items-center justify-center flex text-3xl font-extrabold shadow-lg relative">
                    <!-- dodanie nazwy użytkownika -->
                 </div>
            
                <h2 class="mt-4 font-bold tracking-wide text-floral-white">Nazwa użytkownika</h2>
            </div>
            <!-- Sekcja Nawigacji -->
            <nav>
                <router-link to="/dashboard" class="px-4 py-3 bg-spicy-paprika text-white rounded-lg font-bold shadow-md transition-colors flex items-center gap-3">
                Mój Profil
                </router-link>
                <router-link to="/courses" class="px-4 py-3 hover:bg-carbon-black/50 rounded-lg font-medium transition-colors flex items-center gap-3 text-silver hover:text-floral-white">
                Katalog Kursów
                </router-link>
            </nav>
        </aside>
        <main class="flex-1 overflow-y-auto p-10">
        </main>
    </div>
</template>
