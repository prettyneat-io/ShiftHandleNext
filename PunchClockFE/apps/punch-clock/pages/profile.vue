<template>
  <div class="space-y-6">
    <header class="flex items-center justify-between">
      <div>
        <h1 class="text-3xl font-semibold tracking-tight text-neutral-dark dark:text-text-dark">
          My Profile
        </h1>
        <p class="mt-1 text-sm text-neutral-mid dark:text-text-dark-muted">
          Manage your personal information, security, and account visibility.
        </p>
      </div>
    </header>

    <div class="grid grid-cols-1 gap-6 lg:grid-cols-3">
      <div class="space-y-6 lg:col-span-2">
        <section :class="[cardClass, 'space-y-6']">
          <div class="flex flex-wrap items-start justify-between gap-4 border-b border-neutral-light/60 pb-4 dark:border-border-dark">
            <div class="space-y-1">
              <h2 class="text-xl font-semibold text-neutral-dark dark:text-text-dark">
                Personal Information
              </h2>
              <p class="text-sm text-neutral-mid dark:text-text-dark-muted">
                Keep your contact details current so teams can reach you quickly.
              </p>
            </div>
            <AppButton
              v-if="!editingProfile"
              type="button"
              variant="secondary"
              size="small"
              @click="startEditing"
            >
              Edit Profile
            </AppButton>
          </div>

          <form
            v-if="editingProfile"
            class="space-y-6"
            @submit.prevent="submitProfile"
          >
            <div class="grid grid-cols-1 gap-4 md:grid-cols-2">
              <AppInput
                v-model="firstName"
                id="profile-first-name"
                name="firstName"
                label="First Name"
                :error="profileErrors.firstName"
              />
              <AppInput
                v-model="lastName"
                id="profile-last-name"
                name="lastName"
                label="Last Name"
                :error="profileErrors.lastName"
              />
            </div>

            <div class="grid grid-cols-1 gap-4 md:grid-cols-2">
              <AppInput
                v-model="email"
                id="profile-email"
                name="email"
                label="Email Address"
                type="email"
                :error="profileErrors.email"
              />
              <AppInput
                v-model="phone"
                id="profile-phone"
                name="phone"
                label="Phone"
                :error="profileErrors.phone"
              />
            </div>

            <div class="flex flex-wrap gap-3 pt-2">
              <AppButton
                type="submit"
                variant="primary"
                size="small"
                :loading="isSubmittingProfile"
              >
                {{ isSubmittingProfile ? 'Saving...' : 'Save Changes' }}
              </AppButton>
              <AppButton
                type="button"
                variant="tertiary"
                size="small"
                @click="cancelEditProfile"
              >
                Cancel
              </AppButton>
            </div>
          </form>

          <dl v-else class="grid grid-cols-1 gap-4 md:grid-cols-2">
            <div class="space-y-1">
              <dt class="text-xs font-medium tracking-wide uppercase text-neutral-mid/80 dark:text-text-dark-muted">First Name</dt>
              <dd class="text-base font-medium text-neutral-dark dark:text-text-dark">
                {{ user?.firstName || '—' }}
              </dd>
            </div>
            <div class="space-y-1">
              <dt class="text-xs font-medium tracking-wide uppercase text-neutral-mid/80 dark:text-text-dark-muted">Last Name</dt>
              <dd class="text-base font-medium text-neutral-dark dark:text-text-dark">
                {{ user?.lastName || '—' }}
              </dd>
            </div>
            <div class="space-y-1">
              <dt class="text-xs font-medium tracking-wide uppercase text-neutral-mid/80 dark:text-text-dark-muted">Email</dt>
              <dd class="text-base font-medium text-neutral-dark dark:text-text-dark">
                {{ user?.email || '—' }}
              </dd>
            </div>
            <div class="space-y-1">
              <dt class="text-xs font-medium tracking-wide uppercase text-neutral-mid/80 dark:text-text-dark-muted">Phone</dt>
              <dd class="text-base font-medium text-neutral-dark dark:text-text-dark">
                {{ user?.phone || 'Not provided' }}
              </dd>
            </div>
          </dl>
        </section>

        <section :class="[cardClass, 'space-y-6']">
          <div class="space-y-2">
            <h2 class="text-xl font-semibold text-neutral-dark dark:text-text-dark">
              Change Password
            </h2>
            <p class="text-sm text-neutral-mid dark:text-text-dark-muted">
              Choose a strong password with at least eight characters to keep your account secure.
            </p>
          </div>

          <form class="space-y-5" @submit.prevent="submitPassword">
            <AppInput
              v-model="passwordValues.currentPassword"
              id="current-password"
              name="currentPassword"
              label="Current Password"
              type="password"
              autocomplete="current-password"
              :error="passwordErrors.currentPassword"
            />
            <AppInput
              v-model="passwordValues.newPassword"
              id="new-password"
              name="newPassword"
              label="New Password"
              type="password"
              autocomplete="new-password"
              :error="passwordErrors.newPassword"
            />
            <AppInput
              v-model="passwordValues.confirmPassword"
              id="confirm-password"
              name="confirmPassword"
              label="Confirm New Password"
              type="password"
              autocomplete="new-password"
              :error="passwordErrors.confirmPassword"
            />

            <div class="flex flex-wrap gap-3 pt-1">
              <AppButton
                type="submit"
                variant="primary"
                size="small"
                :loading="isSubmittingPassword"
              >
                {{ isSubmittingPassword ? 'Updating...' : 'Change Password' }}
              </AppButton>
              <p class="text-xs text-neutral-mid dark:text-text-dark-muted">
                Must include at least 8 characters.
              </p>
            </div>
          </form>
        </section>
      </div>

      <div class="space-y-6">
        <section :class="[cardClass, 'text-center space-y-4']">
          <div class="flex justify-center">
            <AppAvatar :name="displayName" size="large" />
          </div>
          <div class="space-y-1">
            <p class="text-lg font-semibold text-neutral-dark dark:text-text-dark">
              {{ displayName }}
            </p>
            <p class="text-sm text-neutral-mid dark:text-text-dark-muted">
              {{ user?.email || 'No email on file' }}
            </p>
          </div>
        </section>

        <section :class="[cardClass, 'space-y-5']">
          <div class="space-y-2">
            <h3 class="text-lg font-semibold text-neutral-dark dark:text-text-dark">
              Account Details
            </h3>
            <p class="text-sm text-neutral-mid dark:text-text-dark-muted">
              Visibility, audit trail, and access levels assigned to you.
            </p>
          </div>

          <dl class="space-y-4">
            <div class="space-y-1">
              <dt class="text-xs font-medium tracking-wide uppercase text-neutral-mid/80 dark:text-text-dark-muted">User ID</dt>
              <dd class="text-sm font-mono tabular-nums text-neutral-dark dark:text-text-dark">
                {{ user?.userId || '—' }}
              </dd>
            </div>
            <div class="space-y-1">
              <dt class="text-xs font-medium tracking-wide uppercase text-neutral-mid/80 dark:text-text-dark-muted">Member Since</dt>
              <dd class="text-sm text-neutral-dark dark:text-text-dark">
                {{ formattedCreatedAt }}
              </dd>
            </div>
            <div class="space-y-1">
              <dt class="text-xs font-medium tracking-wide uppercase text-neutral-mid/80 dark:text-text-dark-muted">Last Active</dt>
              <dd class="text-sm text-neutral-dark dark:text-text-dark">
                {{ formattedLastLogin }}
              </dd>
            </div>
          </dl>

          <div class="space-y-2">
            <p class="text-xs font-medium tracking-wide uppercase text-neutral-mid/80 dark:text-text-dark-muted">
              Assigned Roles
            </p>
            <ul v-if="displayRoles.length" class="flex flex-wrap gap-2">
              <li
                v-for="role in displayRoles"
                :key="role"
                class="rounded-full bg-primary/10 px-3 py-1 text-xs font-semibold text-primary"
              >
                {{ role }}
              </li>
            </ul>
            <p v-else class="text-sm text-neutral-mid dark:text-text-dark-muted">
              No roles assigned yet.
            </p>
          </div>
        </section>

        <section :class="[cardClass, 'space-y-4']">
          <h3 class="text-lg font-semibold text-neutral-dark dark:text-text-dark">
            Quick Actions
          </h3>
          <div class="space-y-2">
            <AppButton
              type="button"
              variant="secondary"
              size="small"
              class="w-full justify-start"
              @click="goTo('/my/attendance')"
            >
              View My Attendance
            </AppButton>
            <AppButton
              type="button"
              variant="secondary"
              size="small"
              class="w-full justify-start"
              @click="goTo('/my/schedule')"
            >
              View My Schedule
            </AppButton>
            <AppButton
              type="button"
              variant="secondary"
              size="small"
              class="w-full justify-start"
              @click="goTo('/my/fingerprint')"
            >
              Manage Fingerprints
            </AppButton>
          </div>
        </section>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useForm } from 'vee-validate'
import { object, ref as yupRef, string } from 'yup'
import { useAuthStore } from '~/stores/auth'
import { useNotificationStore } from '~/stores/notification'
import { usePunchClockApi } from '~/composables/usePunchClockApi'

const authStore = useAuthStore()
const notificationStore = useNotificationStore()
const api = usePunchClockApi()
const router = useRouter()

const user = computed(() => authStore.currentUser)

const cardClass = 'rounded-lg border border-neutral-light/60 bg-white p-6 shadow-lg shadow-neutral-dark/5 transition-all duration-180 ease-out hover:shadow-xl hover:shadow-primary/10 dark:bg-panel-dark dark:border-border-dark'

const phonePattern = /^[\d\s()+-]{0,20}$/

const profileValidationSchema = object({
  firstName: string().trim().required('First name is required'),
  lastName: string().trim().required('Last name is required'),
  email: string().trim().email('Enter a valid email address').required('Email is required'),
  phone: string()
    .nullable()
    .transform((value) => value?.trim() || '')
    .test('phone-format', 'Enter a valid phone number', (value) => !value || phonePattern.test(value || '')),
})

type ProfileFormValues = {
  firstName: string
  lastName: string
  email: string
  phone: string
}

const getInitialProfileValues = (): ProfileFormValues => ({
  firstName: user.value?.firstName ?? '',
  lastName: user.value?.lastName ?? '',
  email: user.value?.email ?? '',
  phone: user.value?.phone ?? '',
})

const {
  handleSubmit: handleProfileSubmit,
  resetForm: resetProfileForm,
  errors: profileErrors,
  values: profileValues,
  isSubmitting: isSubmittingProfile,
  meta: profileMeta,
  setFieldValue,
} = useForm<ProfileFormValues>({
  validationSchema: profileValidationSchema,
  initialValues: getInitialProfileValues(),
})

// Create writable refs that update VeeValidate's field values
const firstName = computed({
  get: () => profileValues.firstName,
  set: (val) => setFieldValue('firstName', val)
})
const lastName = computed({
  get: () => profileValues.lastName,
  set: (val) => setFieldValue('lastName', val)
})
const email = computed({
  get: () => profileValues.email,
  set: (val) => setFieldValue('email', val)
})
const phone = computed({
  get: () => profileValues.phone,
  set: (val) => setFieldValue('phone', val)
})

const editingProfile = ref(false)

const startEditing = () => {
  editingProfile.value = true
  resetProfileForm({ values: getInitialProfileValues() })
}

const cancelEditProfile = () => {
  editingProfile.value = false
  resetProfileForm({ values: getInitialProfileValues() })
}

const submitProfile = handleProfileSubmit(async (values) => {
  if (!user.value?.userId) {
    notificationStore.addNotification({ message: 'Missing user identifier', type: 'error' })
    return
  }

  try {
    await api.updateUsers(user.value.userId, {
      firstName: values.firstName,
      lastName: values.lastName,
      email: values.email,
      phone: values.phone || '',
    })

    await authStore.fetchUser()
    notificationStore.addNotification({ message: 'Your profile details were saved successfully.', type: 'success' })
    editingProfile.value = false
    resetProfileForm({ values: getInitialProfileValues() })
  } catch (error) {
    notificationStore.addNotification({ message: extractErrorMessage(error, 'Failed to update profile details.'), type: 'error' })
  }
})

const passwordValidationSchema = object({
  currentPassword: string().required('Current password is required'),
  newPassword: string().min(8, 'Password must be at least 8 characters long').required('New password is required'),
  confirmPassword: string()
    .required('Please confirm your new password')
    .oneOf([yupRef('newPassword')], 'Passwords must match'),
})

type PasswordFormValues = {
  currentPassword: string
  newPassword: string
  confirmPassword: string
}

const initialPasswordValues: PasswordFormValues = {
  currentPassword: '',
  newPassword: '',
  confirmPassword: '',
}

const {
  handleSubmit: handlePasswordSubmit,
  resetForm: resetPasswordForm,
  errors: passwordErrors,
  values: passwordValues,
  isSubmitting: isSubmittingPassword,
} = useForm<PasswordFormValues>({
  validationSchema: passwordValidationSchema,
  initialValues: initialPasswordValues,
})

const submitPassword = handlePasswordSubmit(async (values) => {
  if (!user.value?.userId) {
    notificationStore.addNotification({ message: 'Missing user identifier', type: 'error' })
    return
  }

  try {
    await api.changePasswordUsers(user.value.userId, {
      currentPassword: values.currentPassword,
      newPassword: values.newPassword,
    })

    notificationStore.addNotification({ message: 'Your password was updated successfully.', type: 'success' })
    resetPasswordForm({ values: initialPasswordValues })
  } catch (error) {
    notificationStore.addNotification({ message: extractErrorMessage(error, 'Unable to update your password right now.'), type: 'error' })
  }
})

const displayName = computed(() => {
  if (!user.value) {
    return 'User'
  }

  const first = user.value.firstName?.trim() ?? ''
  const last = user.value.lastName?.trim() ?? ''
  const fullName = [first, last].filter(Boolean).join(' ')
  return fullName || user.value.username || 'User'
})

const displayRoles = computed(() => {
  if (!user.value) {
    return [] as string[]
  }

  // Check for roles array first (from API response)
  if (Array.isArray(user.value.roles) && user.value.roles.length) {
    const roles = user.value.roles
    // If roles contains objects with roleName, extract the names
    if (typeof roles[0] === 'object' && roles[0] !== null && 'roleName' in roles[0]) {
      return (roles as Array<{ roleName?: string; roleId?: string }>)
        .map((role) => role.roleName || role.roleId || '')
        .filter((role): role is string => !!role && role.trim().length > 0)
    }
    // If roles contains strings directly
    return roles.filter((role): role is string => typeof role === 'string' && role.length > 0)
  }

  // Fall back to userRoles structure
  return (
    user.value.userRoles
      ?.map((role) => role.role?.roleName || role.roleId || '')
      .filter((role): role is string => !!role && role.trim().length > 0)
  ) ?? []
})

const dateFormatter = new Intl.DateTimeFormat('en-US', {
  year: 'numeric',
  month: 'long',
  day: 'numeric',
})

const formatDate = (value: string | Date | null | undefined) => {
  if (!value) {
    return '—'
  }

  const date = value instanceof Date ? value : new Date(value)
  if (Number.isNaN(date.getTime())) {
    return '—'
  }

  return dateFormatter.format(date)
}

const formattedCreatedAt = computed(() => formatDate(user.value?.createdAt ?? null))
const formattedLastLogin = computed(() => formatDate(user.value?.lastLogin ?? null))

const goTo = (path: string) => {
  router.push(path)
}

watch(user, () => {
  if (!editingProfile.value || !profileMeta.value.dirty) {
    resetProfileForm({ values: getInitialProfileValues() })
  }
})

const extractErrorMessage = (error: unknown, fallback: string) => {
  if (error instanceof Error && error.message) {
    return error.message
  }

  if (typeof error === 'object' && error !== null) {
    const maybeMessage = (error as { message?: string }).message
    if (maybeMessage) {
      return maybeMessage
    }
  }

  if (typeof error === 'string') {
    return error
  }

  return fallback
}

watch(editingProfile, (isEditing) => {
  if (!isEditing) {
    resetProfileForm({ values: getInitialProfileValues() })
  }
})
</script>
