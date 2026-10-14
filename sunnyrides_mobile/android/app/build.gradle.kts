plugins {
    id("com.android.application")
    // The Flutter Gradle Plugin must be applied after the Android and Kotlin Gradle plugins.
    id("dev.flutter.flutter-gradle-plugin")
}

android {
    namespace = "ba.edu.fit.sunnyrides_mobile"
    compileSdk = flutter.compileSdkVersion
    ndkVersion = flutter.ndkVersion

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }

    defaultConfig {
        applicationId = "ba.edu.fit.sunnyrides_mobile"
        minSdk = flutter.minSdkVersion
        targetSdk = flutter.targetSdkVersion

        // Broj i naziv verzije dolaze iz pubspec.yaml.
        versionCode = flutter.versionCode
        versionName = flutter.versionName
    }

    buildTypes {
        release {
            // APK se instalira u emulator za pregled rada, ne objavljuje se na Google
            // Play, pa je potpis debug kljucem dovoljan.
            signingConfig = signingConfigs.getByName("debug")
        }
    }

    lint {
        // Release build inace pokrece lint i nad paketima. Za stripe_android lint
        // trazi play-services-tapandpay, biblioteku koja nije u javnim Maven
        // repozitorijima, pa build pada prije nego APK nastane. Aplikaciji ta
        // biblioteka ne treba - koristi je samo dodavanje kartica u Google Wallet.
        checkReleaseBuilds = false
    }
}

kotlin {
    compilerOptions {
        jvmTarget = org.jetbrains.kotlin.gradle.dsl.JvmTarget.JVM_17
    }
}

flutter {
    source = "../.."
}
