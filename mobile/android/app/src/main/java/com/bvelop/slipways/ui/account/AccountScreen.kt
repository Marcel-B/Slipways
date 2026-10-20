package com.bvelop.slipways.ui.account

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material3.Button
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.unit.dp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.lifecycle.viewmodel.compose.viewModel

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun AccountScreen(
    onBack: () -> Unit,
    viewModel: AccountViewModel = viewModel(factory = AccountViewModel.Factory),
) {
    val user by viewModel.user.collectAsStateWithLifecycle()

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("Konto") },
                navigationIcon = {
                    IconButton(onClick = onBack) {
                        Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = "Zurück")
                    }
                },
            )
        },
    ) { padding ->
        Column(
            Modifier.padding(padding).padding(16.dp),
            verticalArrangement = Arrangement.spacedBy(12.dp),
        ) {
            val current = user
            if (current != null) {
                Text("Angemeldet als ${current.displayName ?: current.email}", style = MaterialTheme.typography.titleMedium)
                OutlinedButton(onClick = viewModel::logout) { Text("Abmelden") }
            } else {
                SignInForm(viewModel)
            }
        }
    }
}

@Composable
private fun SignInForm(viewModel: AccountViewModel) {
    val busy by viewModel.busy.collectAsStateWithLifecycle()
    val error by viewModel.error.collectAsStateWithLifecycle()
    var register by rememberSaveable { mutableStateOf(false) }
    var email by rememberSaveable { mutableStateOf("") }
    var password by rememberSaveable { mutableStateOf("") }

    Text(if (register) "Registrieren" else "Anmelden", style = MaterialTheme.typography.headlineSmall)
    OutlinedTextField(
        value = email,
        onValueChange = { email = it },
        label = { Text("E-Mail") },
        singleLine = true,
        keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Email),
        modifier = Modifier.fillMaxWidth(),
    )
    OutlinedTextField(
        value = password,
        onValueChange = { password = it },
        label = { Text("Passwort") },
        singleLine = true,
        visualTransformation = PasswordVisualTransformation(),
        keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Password),
        supportingText = { if (register) Text("Mindestens 8 Zeichen") },
        modifier = Modifier.fillMaxWidth(),
    )
    error?.let { Text(it, color = MaterialTheme.colorScheme.error) }
    Button(
        onClick = { viewModel.submit(email, password, register) },
        enabled = !busy && email.isNotBlank() && password.length >= if (register) 8 else 1,
        modifier = Modifier.fillMaxWidth(),
    ) {
        Text(if (register) "Konto anlegen" else "Anmelden")
    }
    TextButton(onClick = { register = !register }) {
        Text(if (register) "Schon registriert? Anmelden" else "Noch kein Konto? Registrieren")
    }
}
