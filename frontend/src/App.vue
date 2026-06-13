<template>
  <div class="min-h-screen bg-gradient-to-br from-slate-950 via-slate-900 to-slate-950 text-white">
    <div class="mx-auto flex min-h-screen max-w-7xl flex-col px-4 py-5">

      <!-- Header -->
      <header
        class="mb-5 flex items-center justify-between rounded-3xl border border-white/10 bg-white/10 px-6 py-4 shadow-2xl backdrop-blur-xl">


        <button
          class="flex h-11 w-11 items-center justify-center rounded-2xl border border-white/10 bg-white/10 transition hover:bg-white/20"
          title="Theme">
          <i class="fa-solid fa-sun text-yellow-300"></i>
        </button>
      </header>

      <main class="grid flex-1 min-h-0 grid-cols-1 gap-5">

        <div class="grid grid-cols-1 lg:grid-cols-4 gap-6 h-full w-full">

          <section :class="[
            'flex h-[calc(100vh-150px)] min-h-0 flex-col overflow-hidden rounded-3xl border border-white/10 bg-white/10 shadow-2xl backdrop-blur-xl',
            false ? 'lg:col-span-3' : 'lg:col-span-4'
          ]">

            <div class="flex items-center justify-between border-b border-white/10 px-6 py-4">
              <div>
                <h2 class="text-lg font-semibold">
                  <i class="fa-solid fa-comments mr-2 text-blue-400"></i>
                  Conversation
                </h2>
                <p class="text-xs text-slate-400">
                  Connected to ASP.NET Core API
                </p>
              </div>

              <div class="flex items-center gap-3">
                <div
                  class="rounded-full border border-emerald-500/20 bg-emerald-500/10 px-3 py-1 text-xs text-emerald-300">
                  <i class="fa-solid fa-circle mr-1 text-[8px]"></i>
                  Online
                </div>

                <button type="button" @click="clearChatHistory"
                  class="rounded-full border border-red-500/20 bg-red-500/10 px-3 py-1 text-xs text-red-300 transition hover:bg-red-500/20">
                  <i class="fa-solid fa-trash mr-1"></i>
                  Clear
                </button>
              </div>
            </div>

            <div ref="chatBody" class="custom-scrollbar min-h-0 flex-1 space-y-6 overflow-y-auto px-6 py-6">

              <div v-if="messages.length === 0 && !isLoadingHistory"
                class="flex h-full flex-col items-center justify-center text-center">
                <div
                  class="mb-4 flex h-20 w-20 items-center justify-center rounded-3xl border border-blue-400/20 bg-blue-600/20">
                  <i class="fa-solid fa-robot text-4xl text-blue-300"></i>
                </div>

                <h3 class="text-xl font-semibold">
                  Start your AI conversation
                </h3>

                <p class="mt-2 max-w-md text-sm text-slate-400">
                  Send a message or upload an image. Your conversation will be saved in SQL Server.
                </p>
              </div>

              <div v-if="isLoadingHistory" class="flex h-full items-center justify-center text-slate-400">
                <i class="fa-solid fa-spinner animate-spin mr-2"></i>
                Loading chat history...
              </div>

              <template v-for="message in messages" :key="message.id">
                <div v-if="message.role === 'user'" class="flex justify-end">
                  <div class="max-w-[78%]">
                    <div class="mb-2 flex items-center justify-end gap-2 text-xs text-slate-400">
                      <span>You</span>
                      <div class="flex h-7 w-7 items-center justify-center rounded-full bg-blue-600">
                        <i class="fa-solid fa-user text-xs"></i>
                      </div>
                    </div>

                    <div class="rounded-3xl rounded-tr-md bg-blue-600 px-5 py-4 shadow-lg shadow-blue-600/20">
                      <p class="whitespace-pre-line leading-relaxed">
                        {{ message.text }}
                      </p>
                      <img v-if="message.imagePreview" :src="message.imagePreview"
                        class="mt-4 max-h-64 rounded-2xl border border-white/20 shadow-lg" />
                    </div>
                  </div>
                </div>

                <div v-else class="flex justify-start">
                  <div class="max-w-[78%]">
                    <div class="mb-2 flex items-center gap-2 text-xs text-slate-400">
                      <div class="flex h-7 w-7 items-center justify-center rounded-full bg-slate-700">
                        <i class="fa-solid fa-robot text-xs text-blue-300"></i>
                      </div>
                      <span>Argho's Custom AI</span>
                    </div>

                    <div class="rounded-3xl rounded-tl-md border border-white/10 bg-slate-800/90 px-5 py-4 shadow-xl">
                      <p class="whitespace-pre-line leading-relaxed text-slate-100">
                        {{ message.text }}
                      </p>
                      <div class="mt-3 text-[11px] text-slate-500">
                        <i class="fa-regular fa-clock mr-1"></i>
                        {{ message.time }}
                      </div>
                    </div>
                  </div>
                </div>
              </template>

              <div v-if="isSending" class="flex justify-start">
                <div class="rounded-3xl rounded-tl-md border border-white/10 bg-slate-800/90 px-5 py-4">
                  <div class="flex items-center gap-2 text-slate-300">
                    <span class="h-2 w-2 animate-bounce rounded-full bg-blue-400"></span>
                    <span class="h-2 w-2 animate-bounce rounded-full bg-blue-400 [animation-delay:0.15s]"></span>
                    <span class="h-2 w-2 animate-bounce rounded-full bg-blue-400 [animation-delay:0.3s]"></span>
                    <span class="ml-2 text-sm">AI is thinking...</span>
                  </div>
                </div>
              </div>

            </div>

            <div class="border-t border-white/10 bg-slate-950/50 px-5 py-4">
              <div v-if="errorMessage"
                class="mb-3 rounded-2xl border border-red-500/20 bg-red-500/10 px-4 py-3 text-red-300">
                {{ errorMessage }}
              </div>

              <form @submit.prevent="sendMessage">
                <textarea v-model="messageText" rows="3"
                  class="w-full resize-none rounded-2xl border border-white/10 bg-white/10 px-4 py-3 text-slate-100 placeholder:text-slate-500 focus:outline-none focus:ring-2 focus:ring-blue-500"
                  placeholder="Type your message..."></textarea>

                <div class="mt-3 flex items-center gap-3">
                  <label
                    class="flex-1 cursor-pointer rounded-2xl border border-white/10 bg-white/10 px-4 py-3 text-slate-300 transition hover:bg-white/15">
                    <i class="fa-solid fa-image mr-2 text-blue-300"></i>
                    <span>{{ selectedFileName }}</span>
                    <input type="file" accept="image/*" class="hidden" @change="handleFileChange" />
                  </label>

                  <button type="submit" :disabled="isSending"
                    class="rounded-2xl bg-blue-600 px-6 py-3 font-semibold shadow-lg shadow-blue-600/30 transition hover:bg-blue-700 disabled:cursor-not-allowed disabled:opacity-60">
                    <span v-if="!isSending">
                      Send <i class="fa-solid fa-paper-plane ml-2"></i>
                    </span>
                    <span v-else>
                      Sending <i class="fa-solid fa-spinner animate-spin ml-2"></i>
                    </span>
                  </button>
                </div>
              </form>
            </div>
          </section>

        </div>
      </main>
    </div>
  </div>
</template>

<script setup>
function cleanAiText(text) {
  if (!text) {
    return ''
  }

  return text.replace(/^\[[^\]]+\]\s*\n?/, '').trim()
}
import { nextTick, onMounted, ref } from 'vue'

const messageText = ref('')
const selectedFile = ref(null)
const selectedFileName = ref('Upload image')

const isSending = ref(false)
const isLoadingHistory = ref(false)

const errorMessage = ref('')
const chatBody = ref(null)

const messages = ref([])
const providerStatuses = ref([])

function formatTime(dateValue) {
  if (!dateValue) {
    return new Date().toLocaleString()
  }

  return new Date(dateValue).toLocaleString()
}

async function scrollToBottom() {
  await nextTick()

  if (chatBody.value) {
    chatBody.value.scrollTop = chatBody.value.scrollHeight
  }
}

function handleFileChange(event) {
  const file = event.target.files[0]

  selectedFile.value = file || null
  selectedFileName.value = file ? file.name : 'Upload image'
}

async function loadHistory() {
  try {
    isLoadingHistory.value = true

    const response = await fetch('/api/chat/history')

    if (!response.ok) {
      throw new Error('Failed to load chat history.')
    }

    const data = await response.json()

    const loadedMessages = []

    data.forEach(chat => {
      const userMessage =
        chat.userMessage ??
        chat.UserMessage ??
        '[Message unavailable]'

      const aiResponse =
        chat.aiResponse ??
        chat.AiResponse ??
        ''

      const uploadedImagePath =
        chat.uploadedImagePath ??
        chat.UploadedImagePath ??
        null

      const createdAt =
        chat.createdAt ??
        chat.CreatedAt ??
        null

      loadedMessages.push({
        id: `user-${chat.id ?? chat.Id}`,
        role: 'user',
        text: userMessage,
        imagePreview: uploadedImagePath,
        time: formatTime(createdAt)
      })

      loadedMessages.push({
        id: `ai-${chat.id ?? chat.Id}`,
        role: 'ai',
        text: cleanAiText(aiResponse),
        time: formatTime(createdAt)
      })
    })

    messages.value = loadedMessages

    await scrollToBottom()
  } catch (error) {
    errorMessage.value = error.message
  } finally {
    isLoadingHistory.value = false
  }
}

async function loadProviderStatuses() {
  try {
    const response = await fetch('/api/chat/providers/status')

    if (!response.ok) {
      throw new Error('Failed to load provider statuses.')
    }

    providerStatuses.value = await response.json()
  } catch {
    providerStatuses.value = []
  }
}

function getProviderStatusClass(status) {
  if (status === 'Active') {
    return 'bg-emerald-500/10 text-emerald-300 border-emerald-500/20'
  }

  if (status === 'Ready' || status === 'Ready to Try') {
    return 'bg-blue-500/10 text-blue-300 border-blue-500/20'
  }

  if (status === 'Cooldown') {
    return 'bg-yellow-500/10 text-yellow-300 border-yellow-500/20'
  }

  return 'bg-red-500/10 text-red-300 border-red-500/20'
}

function formatCooldown(cooldownUntil) {
  if (!cooldownUntil) {
    return ''
  }

  const until = new Date(cooldownUntil)

  return until.toLocaleTimeString()
}

async function sendMessage() {
  if (!messageText.value.trim() && !selectedFile.value) {
    return
  }

  errorMessage.value = ''
  isSending.value = true

  const localImagePreview = selectedFile.value
    ? URL.createObjectURL(selectedFile.value)
    : null

  const localUserMessage = {
    id: crypto.randomUUID(),
    role: 'user',
    text: messageText.value || '[Image uploaded]',
    imagePreview: localImagePreview,
    time: formatTime()
  }

  messages.value.push(localUserMessage)

  const formData = new FormData()
  formData.append('message', messageText.value)

  if (selectedFile.value) {
    formData.append('imageFile', selectedFile.value)
  }

  messageText.value = ''
  selectedFile.value = null
  selectedFileName.value = 'Upload image'

  await scrollToBottom()

  try {
    const response = await fetch('/api/chat/send', {
      method: 'POST',
      body: formData
    })

    if (!response.ok) {
      const errorData = await response.json()
      throw new Error(errorData.error || 'Message sending failed.')
    }

    const data = await response.json()

    messages.value.push({
      id: `ai-${data.id}`,
      role: 'ai',
      text: cleanAiText(data.aiResponse),
      time: formatTime(data.createdAt)
    })

    await loadProviderStatuses()
    await scrollToBottom()
  } catch (error) {
    errorMessage.value = error.message

    messages.value.push({
      id: crypto.randomUUID(),
      role: 'ai',
      text: 'Something went wrong while contacting the backend API.',
      time: formatTime()
    })

    await scrollToBottom()
  } finally {
    isSending.value = false
    await loadProviderStatuses()
  }
}

async function clearChatHistory() {
  const confirmed = confirm('Are you sure you want to clear all chat history?')

  if (!confirmed) {
    return
  }

  try {
    errorMessage.value = ''

    const response = await fetch('/api/chat/clear', {
      method: 'DELETE'
    })

    if (!response.ok) {
      throw new Error('Failed to clear chat history.')
    }

    messages.value = []

    await loadProviderStatuses()
    await scrollToBottom()
  } catch (error) {
    errorMessage.value = error.message
  }
}

onMounted(() => {
  loadHistory()
  loadProviderStatuses()

  setInterval(() => {
    loadProviderStatuses()
  }, 30000)
})
</script>