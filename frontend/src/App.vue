<template>
  <div
    class="h-screen max-h-screen min-h-0 overflow-hidden bg-gradient-to-br from-slate-950 via-slate-900 to-slate-950 text-white">
    <div class="mx-auto flex h-full max-w-7xl flex-col px-4 py-5">
      <header
        class="mb-5 flex flex-shrink-0 items-center justify-between rounded-3xl border border-white/10 bg-white/10 px-6 py-4 shadow-2xl backdrop-blur-xl">
        <div class="flex items-center gap-2">
          <span class="inline-block h-3 w-3 animate-pulse rounded-full bg-blue-500"></span>
          <span class="text-sm font-medium text-slate-300">AI Dashboard</span>
        </div>

        <div class="flex items-center gap-3">
          <button type="button" @click="createNewConversation" :disabled="isCreatingConversation"
            class="flex items-center gap-2 rounded-2xl border border-blue-500/20 bg-blue-500/10 px-4 py-2 text-sm text-blue-300 transition hover:bg-blue-500/20 disabled:cursor-not-allowed disabled:opacity-60">
            <i v-if="!isCreatingConversation" class="fa-solid fa-plus"></i>
            <i v-else class="fa-solid fa-spinner animate-spin"></i>
            <span>New Chat</span>
          </button>

          <button
            class="flex h-11 w-11 items-center justify-center rounded-2xl border border-white/10 bg-white/10 transition hover:bg-white/20"
            title="Theme">
            <i class="fa-solid fa-sun text-yellow-300"></i>
          </button>
        </div>
      </header>

      <main class="grid min-h-0 flex-1 grid-cols-1 gap-6 lg:grid-cols-4">
        <section
          class="flex h-full min-h-0 flex-col overflow-hidden rounded-3xl border border-white/10 bg-white/10 shadow-2xl backdrop-blur-xl lg:col-span-3">
          <div class="flex flex-shrink-0 items-center justify-between border-b border-white/10 px-6 py-4">
            <div>
              <h2 class="text-lg font-semibold">
                <i class="fa-solid fa-comments mr-2 text-blue-400"></i>
                {{ activeConversationTitle }}
              </h2>
              <p class="text-xs text-slate-400">
                Conversation ID:
                <span v-if="activeConversationId">{{ activeConversationId }}</span>
                <span v-else>Not selected</span>
              </p>
            </div>

            <div class="flex items-center gap-3">
              <button v-if="activeConversationId && conversations.length > 0" type="button"
                @click="deleteActiveConversation"
                class="rounded-full border border-red-500/20 bg-red-500/10 px-3 py-1 text-xs text-red-300 transition hover:bg-red-500/20">
                <i class="fa-solid fa-trash mr-1"></i>
                Delete
              </button>

              <div
                class="rounded-full border border-emerald-500/20 bg-emerald-500/10 px-3 py-1 text-xs text-emerald-300">
                <i class="fa-solid fa-circle mr-1 text-[8px]"></i>
                Online
              </div>
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
                Start this conversation
              </h3>

              <p class="mt-2 max-w-md text-sm text-slate-400">
                Send a message or upload an image. This conversation will be saved separately.
              </p>
            </div>

            <div v-if="isLoadingHistory" class="flex h-full items-center justify-center text-slate-400">
              <i class="fa-solid fa-spinner animate-spin mr-2"></i>
              Loading conversation...
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
                    <div class="chat-markdown chat-markdown-user" v-html="renderMarkdown(message.text)"></div>

                    <img v-if="message.imagePreview" :src="message.imagePreview"
                      class="mt-4 max-h-64 rounded-2xl border border-white/20 shadow-lg" />

                    <div class="mt-3 text-right text-[11px] text-blue-100/80">
                      <i class="fa-regular fa-clock mr-1"></i>
                      {{ message.time }}
                    </div>
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

                    <span v-if="message.providerName"
                      class="rounded-full border border-blue-500/20 bg-blue-500/10 px-2 py-0.5 text-[10px] text-blue-300">
                      {{ message.providerName }}
                    </span>
                  </div>

                  <div class="rounded-3xl rounded-tl-md border border-white/10 bg-slate-800/90 px-5 py-4 shadow-xl">
                    <div class="chat-markdown" v-html="renderMarkdown(message.text)"></div>

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
                  <span class="ml-2 text-sm">
                    AI is thinking...
                  </span>
                </div>
              </div>
            </div>
          </div>

          <div class="flex-shrink-0 border-t border-white/10 bg-slate-950/50 px-5 py-4">
            <div v-if="errorMessage"
              class="mb-3 rounded-2xl border border-red-500/20 bg-red-500/10 px-4 py-3 text-red-300">
              {{ errorMessage }}
            </div>

            <form @submit.prevent="sendMessage">
              <textarea v-model="messageText" rows="2"
                class="w-full resize-none rounded-2xl border border-white/10 bg-white/10 px-4 py-3 text-slate-100 placeholder:text-slate-500 focus:outline-none focus:ring-2 focus:ring-blue-500"
                placeholder="Type your message..."></textarea>

              <div class="mt-2 flex items-center gap-3">
                <label
                  class="flex-1 cursor-pointer rounded-2xl border border-white/10 bg-white/10 px-4 py-2.5 text-sm text-slate-300 transition hover:bg-white/15">
                  <i class="fa-solid fa-image mr-2 text-blue-300"></i>
                  <span>{{ selectedFileName }}</span>
                  <input ref="fileInput" type="file" accept="image/*" class="hidden" @change="handleFileChange" />
                </label>

                <button type="submit" :disabled="isSending || !activeConversationId"
                  class="rounded-2xl bg-blue-600 px-6 py-2.5 text-sm font-semibold shadow-lg shadow-blue-600/30 transition hover:bg-blue-700 disabled:cursor-not-allowed disabled:opacity-60">
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

        <aside class="space-y-5 lg:col-span-1 min-h-0 flex flex-col h-full overflow-hidden">
          <div class="flex-1 min-h-0 overflow-y-auto custom-scrollbar space-y-5">
            <div class="rounded-3xl border border-white/10 bg-white/10 p-5 shadow-2xl backdrop-blur-xl">
              <div class="mb-4 flex items-center justify-between gap-3">
                <h2 class="text-sm font-bold tracking-wider text-slate-400 uppercase">
                  <i class="fa-solid fa-history mr-2 text-blue-400"></i>
                  Recent Chats
                </h2>

                <button type="button" @click="loadConversations(false)"
                  class="text-xs text-blue-300 hover:text-blue-200" title="Refresh">
                  <i class="fa-solid fa-rotate-right"></i>
                </button>
              </div>

              <div v-if="isLoadingConversations" class="py-4 text-center text-xs text-slate-400">
                <i class="fa-solid fa-spinner animate-spin mr-2"></i>
                Loading chats...
              </div>

              <div v-else-if="conversations.length === 0"
                class="text-xs text-slate-500 py-2 text-center border border-dashed border-white/5 rounded-xl">
                No recent conversations
              </div>

              <div v-else class="space-y-2">
                <button v-for="conversation in conversations" :key="conversation.id"
                  @click="selectConversation(conversation)"
                  class="w-full text-left rounded-xl px-3 py-2.5 text-xs transition border flex items-center gap-2"
                  :class="conversation.id === activeConversationId
                    ? 'bg-blue-500/15 border-blue-500/30'
                    : 'bg-white/5 hover:bg-white/10 border-white/5'">
                  <i class="fa-regular fa-message text-sm"
                    :class="conversation.id === activeConversationId ? 'text-blue-300' : 'text-slate-400'"></i>

                  <div class="truncate flex-1">
                    <p class="font-medium truncate text-slate-200">
                      {{ conversation.title }}
                    </p>

                    <span class="block truncate text-[10px] text-slate-500">
                      {{ conversation.lastMessage || 'No messages yet' }}
                    </span>

                    <span class="text-[10px] text-slate-600">
                      {{ formatTime(conversation.updatedAt) }}
                    </span>
                  </div>
                </button>
              </div>
            </div>

            <div class="rounded-3xl border border-white/10 bg-white/10 p-5 shadow-2xl backdrop-blur-xl">
              <h2 class="mb-4 text-lg font-bold">
                <i class="fa-solid fa-signal mr-2 text-emerald-300"></i>
                Provider Status
              </h2>

              <div v-if="providerStatuses.length === 0" class="text-sm text-slate-400">
                Provider status unavailable.
              </div>

              <div v-else class="space-y-3">
                <div v-for="provider in providerStatuses" :key="provider.name"
                  class="rounded-2xl border border-white/10 bg-white/10 px-4 py-3">
                  <div class="flex items-center justify-between gap-3">
                    <div>
                      <div class="font-semibold text-slate-100 text-sm">
                        {{ provider.name }}
                      </div>
                      <div class="text-[11px] text-slate-400">
                        Priority {{ provider.priority }}
                      </div>
                    </div>

                    <span class="rounded-full border px-2.5 py-0.5 text-[11px]"
                      :class="getProviderStatusClass(provider.status)">
                      {{ provider.status }}
                    </span>
                  </div>

                  <div v-if="provider.cooldownUntil" class="mt-2 text-xs text-yellow-300">
                    <i class="fa-regular fa-clock mr-1"></i>
                    Retry after {{ formatCooldown(provider.cooldownUntil) }}
                  </div>
                </div>
              </div>
            </div>

            <div class="rounded-3xl border border-white/10 bg-white/10 p-5 shadow-2xl backdrop-blur-xl">
              <h2 class="mb-2 text-sm font-bold text-slate-400 uppercase tracking-wider">
                <i class="fa-solid fa-server mr-2 text-blue-300"></i>
                System Architecture
              </h2>

              <div class="space-y-1 text-xs text-slate-400 mt-3">
                <p>
                  <span class="text-slate-300 font-medium">Backend:</span>
                  ASP.NET Core API
                </p>
                <p>
                  <span class="text-slate-300 font-medium">Database:</span>
                  SQL Server
                </p>
                <p>
                  <span class="text-slate-300 font-medium">Text AI:</span>
                  Multi-provider fallback
                </p>
                <p>
                  <span class="text-slate-300 font-medium">Vision AI:</span>
                  Gemini + GitHub Models
                </p>
                <p>
                  <span class="text-slate-300 font-medium">Chats:</span>
                  Conversation-based
                </p>
              </div>
            </div>
          </div>
        </aside>
      </main>
    </div>
  </div>
</template>

<script setup>
import { nextTick, onBeforeUnmount, onMounted, ref } from 'vue'
import MarkdownIt from 'markdown-it'
import DOMPurify from 'dompurify'

const markdownRenderer = new MarkdownIt({
  html: false,
  linkify: true,
  breaks: true,
  typographer: true
})

function renderMarkdown(text) {
  if (!text) {
    return ''
  }

  const cleanedText = cleanAiText(text)
  const html = markdownRenderer.render(cleanedText)

  return DOMPurify.sanitize(html)
}

const messageText = ref('')
const selectedFile = ref(null)
const selectedFileName = ref('Upload image')
const fileInput = ref(null)

const isSending = ref(false)
const isLoadingHistory = ref(false)
const isLoadingConversations = ref(false)
const isCreatingConversation = ref(false)

const errorMessage = ref('')
const chatBody = ref(null)

const messages = ref([])
const conversations = ref([])
const providerStatuses = ref([])

const activeConversationId = ref(null)
const activeConversationTitle = ref('New Chat')

let providerStatusInterval = null
const localPreviewUrls = []

function cleanAiText(text) {
  if (!text) {
    return ''
  }

  return text.replace(/^\[[^\]]+\]\s*\n?/, '').trim()
}

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

function resetFileInput() {
  selectedFile.value = null
  selectedFileName.value = 'Upload image'

  if (fileInput.value) {
    fileInput.value.value = ''
  }
}

async function loadConversations(autoSelectLatest = false) {
  try {
    isLoadingConversations.value = true

    const response = await fetch('/api/chat/conversations')

    if (!response.ok) {
      throw new Error('Failed to load recent chats.')
    }

    const data = await response.json()

    conversations.value = data

    const activeConversation = conversations.value.find(
      conversation => conversation.id === activeConversationId.value
    )

    if (activeConversation) {
      activeConversationTitle.value = activeConversation.title
    }

    if (autoSelectLatest) {
      if (conversations.value.length > 0) {
        await selectConversation(conversations.value[0])
      } else {
        await createNewConversation()
      }
    }
  } catch (error) {
    errorMessage.value = error.message
  } finally {
    isLoadingConversations.value = false
  }
}

async function createNewConversation() {
  if (
    activeConversationId.value &&
    messages.value.length === 0 &&
    activeConversationTitle.value === 'New Chat'
  ) {
    return
  }

  try {
    isCreatingConversation.value = true
    errorMessage.value = ''

    const response = await fetch('/api/chat/conversations', {
      method: 'POST'
    })

    if (!response.ok) {
      throw new Error('Failed to create new chat.')
    }

    const data = await response.json()

    activeConversationId.value = data.id
    activeConversationTitle.value = data.title || 'New Chat'
    messages.value = []

    await loadConversations(false)
    await scrollToBottom()
  } catch (error) {
    errorMessage.value = error.message
  } finally {
    isCreatingConversation.value = false
  }
}

async function selectConversation(conversation) {
  if (!conversation || !conversation.id) {
    return
  }

  activeConversationId.value = conversation.id
  activeConversationTitle.value = conversation.title || 'New Chat'

  await loadHistory(conversation.id)
}

async function loadHistory(conversationId = activeConversationId.value) {
  if (!conversationId) {
    messages.value = []
    return
  }

  try {
    isLoadingHistory.value = true
    errorMessage.value = ''

    const response = await fetch(`/api/chat/history?conversationId=${encodeURIComponent(conversationId)}`)

    if (!response.ok) {
      throw new Error('Failed to load conversation history.')
    }

    const data = await response.json()
    const loadedMessages = []

    data.forEach(chat => {
      const userMessage = chat.userMessage ?? chat.UserMessage ?? '[Message unavailable]'
      const aiResponse = chat.aiResponse ?? chat.AiResponse ?? ''
      const uploadedImagePath = chat.uploadedImagePath ?? chat.UploadedImagePath ?? null
      const createdAt = chat.createdAt ?? chat.CreatedAt ?? null

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
        providerName: null,
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

async function deleteActiveConversation() {
  if (!activeConversationId.value) {
    return
  }

  const confirmed = confirm('Delete this conversation? This will not delete other recent chats.')

  if (!confirmed) {
    return
  }

  try {
    errorMessage.value = ''

    const response = await fetch(`/api/chat/conversations/${activeConversationId.value}`, {
      method: 'DELETE'
    })

    if (!response.ok) {
      throw new Error('Failed to delete conversation.')
    }

    activeConversationId.value = null
    activeConversationTitle.value = 'New Chat'
    messages.value = []

    await loadConversations(true)
  } catch (error) {
    errorMessage.value = error.message
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

  if (!activeConversationId.value) {
    await createNewConversation()
  }

  if (!activeConversationId.value) {
    errorMessage.value = 'No active conversation found.'
    return
  }

  errorMessage.value = ''
  isSending.value = true

  const localImagePreview = selectedFile.value
    ? URL.createObjectURL(selectedFile.value)
    : null

  if (localImagePreview) {
    localPreviewUrls.push(localImagePreview)
  }

  const localUserMessage = {
    id: crypto.randomUUID(),
    role: 'user',
    text: messageText.value || '[Image uploaded]',
    imagePreview: localImagePreview,
    time: formatTime()
  }

  messages.value.push(localUserMessage)

  const formData = new FormData()

  formData.append('conversationId', activeConversationId.value)
  formData.append('message', messageText.value)

  if (selectedFile.value) {
    formData.append('imageFile', selectedFile.value)
  }

  messageText.value = ''
  resetFileInput()

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

    if (data.conversationId) {
      activeConversationId.value = data.conversationId
    }

    if (data.conversationTitle) {
      activeConversationTitle.value = data.conversationTitle
    }

    if (data.uploadedImagePath && localImagePreview) {
      localUserMessage.imagePreview = data.uploadedImagePath
      URL.revokeObjectURL(localImagePreview)
    }

    messages.value.push({
      id: `ai-${data.id}`,
      role: 'ai',
      text: cleanAiText(data.aiResponse),
      providerName: data.providerName || null,
      time: formatTime(data.createdAt)
    })

    await loadConversations(false)
    await loadProviderStatuses()
    await scrollToBottom()
  } catch (error) {
    errorMessage.value = error.message

    messages.value.push({
      id: crypto.randomUUID(),
      role: 'ai',
      text: 'Something went wrong while contacting the backend API.',
      providerName: null,
      time: formatTime()
    })

    await scrollToBottom()
  } finally {
    isSending.value = false
    await loadProviderStatuses()
  }
}

onMounted(async () => {
  await loadConversations(true)
  await loadProviderStatuses()

  providerStatusInterval = setInterval(() => {
    loadProviderStatuses()
  }, 30000)
})

onBeforeUnmount(() => {
  if (providerStatusInterval) {
    clearInterval(providerStatusInterval)
  }

  localPreviewUrls.forEach(url => {
    URL.revokeObjectURL(url)
  })
})
</script>