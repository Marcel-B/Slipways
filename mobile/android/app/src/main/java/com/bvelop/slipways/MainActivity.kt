package com.bvelop.slipways

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import com.bvelop.slipways.ui.SlipwaysApp
import com.bvelop.slipways.ui.theme.SlipwaysTheme

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()
        setContent {
            SlipwaysTheme {
                SlipwaysApp()
            }
        }
    }
}
