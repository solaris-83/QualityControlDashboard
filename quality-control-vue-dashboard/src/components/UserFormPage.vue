<template>

<div class="page">

    <h1>User Form</h1>

    <div v-if="loading">
        Loading...
    </div>

    <div v-else>

        <div class="form-row">

            <label>Id</label>

            <input
                type="number"
                v-model="user.id" />
        </div>

        <div class="form-row">

            <label>First Name</label>

            <input
                type="text"
                v-model="user.firstName" />
        </div>

        <div class="form-row">

            <label>Last Name</label>

            <input
                type="text"
                v-model="user.lastName" />
        </div>

        <div class="form-row">

            <label>Email</label>

            <input
                type="email"
                v-model="user.email" />
        </div>

        <div class="form-row">

            <label>Age</label>

            <input
                type="number"
                v-model="user.age" />
        </div>

        <div
            v-if="errors.length"
            class="validation">

            <ul>

                <li
                    v-for="e in errors"
                    :key="e">

                    {{ e }}

                </li>

            </ul>

        </div>

        <div class="buttons">

            <button
                @click="loadUser(1)">

                Load
            </button>

            <button
                @click="saveUser"
                :disabled="saving">

                Save
            </button>

            <button
                @click="clear">

                Clear
            </button>
            <button
                @click="uploadDefaultFile()"
                :disabled="loading">

                Upload
            </button>
        </div>

    </div>

</div>

</template>

<script setup lang="ts">

import { useUserForm }
from "../composables/useUserForm"
import { FileRequestDto } from "../models/file-request-dto";

const {
    user,
    loading,
    saving,
    errors,
    loadUser,
    saveUser,
    uploadFile,
    clear
}
=
useUserForm()

function uploadDefaultFile() {

    const request = new FileRequestDto()
    request.week = 22
    request.year = 2026
    request.projects = ["BUS_ADAS", "TRUCK_MH24"]

    uploadFile(request)
}

</script>

<style scoped>

.page {
    max-width: 600px;
    margin: auto;
    padding: 20px;
}

.form-row {
    display: flex;
    flex-direction: column;
    margin-bottom: 12px;
}

.form-row label {
    font-weight: bold;
    margin-bottom: 4px;
}

.form-row input {
    padding: 8px;
}

.validation {
    margin-top: 16px;
    color: red;
}

.buttons {
    margin-top: 20px;
    display: flex;
    gap: 10px;
}

button {
    padding: 10px 16px;
}

</style>