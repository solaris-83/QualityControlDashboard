import { ref, computed } from "vue"
import { User } from "../models/user"
import { bus } from "../services/webviewMessenger"

export function useUserForm() {

    const loading = ref(false)

    const saving = ref(false)

    const user = ref<User>({
        id: 0,
        firstName: "",
        lastName: "",
        email: "",
        age: 18
    })

    const errors = ref<string[]>([])

    const isValid = computed(() => {

        errors.value = []

        if (!user.value.firstName)
            errors.value.push("First Name required")

        if (!user.value.lastName)
            errors.value.push("Last Name required")

        if (!user.value.email)
            errors.value.push("Email required")

        return errors.value.length === 0
    })

    async function loadUser(id: number) {

        loading.value = true

        try {

            const result =
                await bus.request<User>(
                    "user.get",
                    { id })

            user.value = result
        }
        finally {

            loading.value = false
        }
    }

    async function saveUser() {

        if (!isValid.value)
            return

        saving.value = true

        try {

            await bus.request(
                "user.save",
                user.value)

            alert("Saved")
        }
        finally {

            saving.value = false
        }
    }

    function clear() {

        user.value = {
            id: 0,
            firstName: "",
            lastName: "",
            email: "",
            age: 18
        }
    }

    return {
        user,
        loading,
        saving,
        errors,
        isValid,
        loadUser,
        saveUser,
        clear
    }
}