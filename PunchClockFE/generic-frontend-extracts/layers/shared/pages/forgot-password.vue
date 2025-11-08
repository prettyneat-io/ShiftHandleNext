<template>
  <div>
    <div class="sm:mx-auto sm:w-full sm:max-w-sm">
      <img class="mx-auto h-10 w-auto" src="/logo.png" alt="Your Company Logo" />
      <h2 class="mt-10 text-center text-2xl font-bold leading-9 tracking-tight text-gray-900 dark:text-white">Forgot your password?</h2>
       <p class="mt-2 text-center text-sm text-gray-600 dark:text-gray-400">
        Enter your email and we'll send you a reset link.
      </p>
    </div>

    <div class="mt-10 sm:mx-auto sm:w-full sm:max-w-sm">
      <div v-if="emailSent" class="rounded-md bg-green-500/10 p-4 text-center text-green-700 dark:text-green-300">
        <p>A password reset link has been sent to your email address if it exists in our system.</p>
      </div>
      <form v-else class="space-y-6" @submit.prevent="submitForm">
        <div>
          <label for="email" class="block text-sm font-medium leading-6 text-gray-900 dark:text-white">Email address</label>
          <div class="mt-2">
            <input
              v-model="email"
              id="email"
              name="email"
              type="email"
              autocomplete="email"
              required
              class="block w-full rounded-md border-0 py-1.5 text-gray-900 shadow-sm ring-1 ring-inset ring-gray-300 placeholder:text-gray-400 focus:ring-2 focus:ring-inset focus:ring-primary sm:text-sm sm:leading-6 dark:bg-white/5 dark:text-white dark:ring-white/10 dark:focus:ring-primary"
            />
             <span v-if="errors.email" class="mt-1 text-sm text-red-500">{{ errors.email }}</span>
          </div>
        </div>

        <div>
          <AppButton
            variant="primary"
            :loading="loading"
            :disabled="!isFormValid || loading"
            class="w-full"
          >
            Send Reset Link
          </AppButton>
        </div>
      </form>

      <p class="mt-10 text-center text-sm text-gray-500 dark:text-gray-400">
        Remembered your password?
        {{ ' ' }}
        <NuxtLink to="/login" class="font-semibold text-primary hover:text-primary-light dark:text-primary-light dark:hover:text-primary">Sign in</NuxtLink>
      </p>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import { useForm, useField } from 'vee-validate';
import * as yup from 'yup';
import { useNotificationStore } from '../stores/notification';

definePageMeta({
  layout: 'auth',
});

const loading = ref(false);
const emailSent = ref(false);
const notificationStore = useNotificationStore();

const { handleSubmit, errors, meta } = useForm({
  validationSchema: yup.object({
    email: yup.string().email('Invalid email').required('Email is required'),
  }),
});

const { value: email } = useField<string>('email');
const isFormValid = computed(() => meta.value.valid);

const submitForm = handleSubmit(async (values) => {
    loading.value = true;
    console.log('Password reset request sent for:', values.email);
    // NOTE: Implement actual API call here.
    // For now, we'll simulate a successful request.
    await new Promise(resolve => setTimeout(resolve, 1000));

    loading.value = false;
    emailSent.value = true;
    notificationStore.addNotification({
        message: 'Password reset link sent.',
        type: 'success',
    });
});
</script>
