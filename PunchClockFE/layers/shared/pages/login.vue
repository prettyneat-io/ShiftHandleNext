<template>
  <div>
    <div class="sm:mx-auto sm:w-full sm:max-w-sm">
      <img class="mx-auto h-10 w-auto" src="/logo.png" alt="Your Company Logo" />
      <h2 class="mt-10 text-center text-2xl font-bold leading-9 tracking-tight text-gray-900 dark:text-white">Sign in to your account</h2>
    </div>

    <div class="mt-10 sm:mx-auto sm:w-full sm:max-w-sm">
      <form class="space-y-6" @submit.prevent="submitForm">
        <p v-if="errorMsg" class="rounded-md bg-red-500/10 p-3 text-center text-sm text-red-600 dark:text-red-400">
          {{ errorMsg }}
        </p>
        <div>
          <label for="email" class="block text-sm font-medium leading-6 text-gray-900 dark:text-white">Email address</label>
          <div class="mt-2">
            <input v-model="email" id="email" name="email" type="email" autocomplete="email" required class="block w-full rounded-md border-0 py-1.5 text-gray-900 shadow-sm ring-1 ring-inset ring-gray-300 placeholder:text-gray-400 focus:ring-2 focus:ring-inset focus:ring-primary sm:text-sm sm:leading-6 dark:bg-white/5 dark:text-white dark:ring-white/10 dark:focus:ring-primary" />
            <span v-if="errors.email" class="mt-1 text-sm text-red-500">{{ errors.email }}</span>
          </div>
        </div>

        <div>
          <div class="flex items-center justify-between">
            <label for="password" class="block text-sm font-medium leading-6 text-gray-900 dark:text-white">Password</label>
            <div class="text-sm">
              <NuxtLink to="/forgot-password" class="font-semibold text-primary hover:text-primary-light dark:text-primary-light dark:hover:text-primary">Forgot password?</NuxtLink>
            </div>
          </div>
          <div class="mt-2">
            <input v-model="password" id="password" name="password" type="password" autocomplete="current-password" required class="block w-full rounded-md border-0 py-1.5 text-gray-900 shadow-sm ring-1 ring-inset ring-gray-300 placeholder:text-gray-400 focus:ring-2 focus:ring-inset focus:ring-primary sm:text-sm sm:leading-6 dark:bg-white/5 dark:text-white dark:ring-white/10 dark:focus:ring-primary" />
             <span v-if="errors.password" class="mt-1 text-sm text-red-500">{{ errors.password }}</span>
          </div>
        </div>

        <div>
          <AppButton
            variant="primary"
            :loading="loading"
            :disabled="!isFormValid || loading"
            class="w-full"
          >
            Sign in
          </AppButton>
        </div>
      </form>

      <p class="mt-10 text-center text-sm text-gray-500 dark:text-gray-400">
        Not a member?
        {{ ' ' }}
        <NuxtLink to="/signup" class="font-semibold text-primary hover:text-primary-light dark:text-primary-light dark:hover:text-primary">Sign up</NuxtLink>
      </p>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import { useForm, useField } from 'vee-validate';
import * as yup from 'yup';
import { useAuthStore } from '../stores/auth';

definePageMeta({
  layout: 'auth',
});

const authStore = useAuthStore();
const router = useRouter();
const loading = ref(false);

const { handleSubmit, errors, meta } = useForm({
  validationSchema: yup.object({
    email: yup.string().email('Invalid email').required('Email is required'),
    password: yup.string().min(6, 'Password must be at least 6 characters').required('Password is required'),
  }),
});

const { value: email } = useField<string>('email');
const { value: password } = useField<string>('password');

const isFormValid = computed(() => meta.value.valid);
const errorMsg = ref('');

const submitForm = handleSubmit(async (values) => {
  errorMsg.value = '';
  loading.value = true;
  try {
    await authStore.login(values);
    await router.push('/');
  } catch (err: any) {
    errorMsg.value = err.data?.error || 'An unexpected error occurred.';
  } finally {
    loading.value = false;
  }
});
</script>
