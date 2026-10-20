package com.bvelop.slipways.ui

import androidx.compose.runtime.Composable
import androidx.navigation.NavType
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.rememberNavController
import androidx.navigation.navArgument
import com.bvelop.slipways.ui.details.SlipwayDetailsScreen
import com.bvelop.slipways.ui.list.SlipwayListScreen

private object Routes {
    const val LIST = "slipways"
    const val DETAILS = "slipways/{id}"

    fun details(id: String) = "slipways/$id"
}

@Composable
fun SlipwaysApp() {
    val navController = rememberNavController()

    NavHost(navController = navController, startDestination = Routes.LIST) {
        composable(Routes.LIST) {
            SlipwayListScreen(
                onSlipwayClick = { navController.navigate(Routes.details(it)) },
            )
        }
        composable(Routes.DETAILS, arguments = listOf(navArgument("id") { type = NavType.StringType })) {
            SlipwayDetailsScreen(onBack = { navController.popBackStack() })
        }
    }
}
